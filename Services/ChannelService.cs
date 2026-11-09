using System.Text;
using AplicacionTV.Models;

namespace AplicacionTV.Services;

/// <summary>
/// Descarga las listas públicas de iptv-org (https://github.com/iptv-org/iptv).
/// No usa base de datos: guarda una copia en caché como archivo de texto y los
/// favoritos como un pequeño archivo .m3u en el almacenamiento de la app.
/// </summary>
public class ChannelService
{
    const string BaseUrl = "https://iptv-org.github.io/iptv/";
    static readonly TimeSpan CacheLifetime = TimeSpan.FromHours(6);

    public static ChannelService Instance { get; } = new();

    readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(40) };
    readonly Dictionary<string, List<Channel>> _memory = new();
    readonly Dictionary<string, Channel> _favorites = new();
    bool _favoritesLoaded;

    static readonly string[] FootballKeywords =
    {
        "fut", "fút", "football", "soccer", "liga", "league", "calcio", "fussball", "fußball",
        "goal", " gol ", "gol tv", "goltv", "premier", "bundesliga", "serie a", "laliga", "ligue",
        "eredivisie", "copa", "uefa", "fifa", "conmebol", "concacaf", "tudn", "espn", "fox sports",
        "bein", "dazn", "tyc", "win sports", "claro sports", "sky sport", "eurosport", "arena sport",
        "sport tv", "real madrid", "barça", "barca", "juventus", "chelsea", "liverpool", "arsenal",
        "man utd", "benfica", "boca", "flamengo", "fc ", " fc", "stadium", "estadio", "mls", "liga mx",
    };

    public IReadOnlyList<Category> Categories { get; } = new List<Category>
    {
        new() { Id = "futbol", Name = "Fútbol", Icon = "⚽", Color = Color.FromArgb("#16A34A"), Kind = CategoryKind.Football },
        new() { Id = "sports", Name = "Deportes", Icon = "🏆", Color = Color.FromArgb("#2563EB") },
        new() { Id = "favoritos", Name = "Favoritos", Icon = "⭐", Color = Color.FromArgb("#CA8A04"), Kind = CategoryKind.Favorites },
        new() { Id = "espanol", Name = "En Español", Icon = "🗣️", Color = Color.FromArgb("#DC2626"), Kind = CategoryKind.Spanish },
        new() { Id = "entertainment", Name = "Entretenimiento", Icon = "🎭", Color = Color.FromArgb("#9333EA") },
        new() { Id = "movies", Name = "Películas", Icon = "🎬", Color = Color.FromArgb("#B91C1C") },
        new() { Id = "series", Name = "Series", Icon = "📺", Color = Color.FromArgb("#7C3AED") },
        new() { Id = "news", Name = "Noticias", Icon = "📰", Color = Color.FromArgb("#0F766E") },
        new() { Id = "kids", Name = "Infantil", Icon = "🧸", Color = Color.FromArgb("#EA580C") },
        new() { Id = "animation", Name = "Animación", Icon = "🐭", Color = Color.FromArgb("#DB2777") },
        new() { Id = "music", Name = "Música", Icon = "🎵", Color = Color.FromArgb("#C026D3") },
        new() { Id = "documentary", Name = "Documentales", Icon = "🌍", Color = Color.FromArgb("#4D7C0F") },
        new() { Id = "comedy", Name = "Comedia", Icon = "😂", Color = Color.FromArgb("#D97706") },
        new() { Id = "general", Name = "General", Icon = "📡", Color = Color.FromArgb("#475569") },
        new() { Id = "family", Name = "Familia", Icon = "👨‍👩‍👧", Color = Color.FromArgb("#0891B2") },
        new() { Id = "education", Name = "Educación", Icon = "🎓", Color = Color.FromArgb("#1D4ED8") },
        new() { Id = "science", Name = "Ciencia", Icon = "🔬", Color = Color.FromArgb("#0369A1") },
        new() { Id = "cooking", Name = "Cocina", Icon = "🍳", Color = Color.FromArgb("#B45309") },
        new() { Id = "travel", Name = "Viajes", Icon = "✈️", Color = Color.FromArgb("#0E7490") },
        new() { Id = "lifestyle", Name = "Estilo de vida", Icon = "🌿", Color = Color.FromArgb("#15803D") },
        new() { Id = "culture", Name = "Cultura", Icon = "🎨", Color = Color.FromArgb("#A21CAF") },
        new() { Id = "classic", Name = "Clásicos", Icon = "📼", Color = Color.FromArgb("#78716C") },
        new() { Id = "auto", Name = "Motor", Icon = "🏎️", Color = Color.FromArgb("#991B1B") },
        new() { Id = "outdoor", Name = "Aire libre", Icon = "🎣", Color = Color.FromArgb("#3F6212") },
        new() { Id = "business", Name = "Negocios", Icon = "💼", Color = Color.FromArgb("#334155") },
        new() { Id = "weather", Name = "Clima", Icon = "⛅", Color = Color.FromArgb("#0284C7") },
        new() { Id = "relax", Name = "Relajación", Icon = "🧘", Color = Color.FromArgb("#0D9488") },
        new() { Id = "religious", Name = "Religión", Icon = "🙏", Color = Color.FromArgb("#6D28D9") },
        new() { Id = "public", Name = "Públicos", Icon = "🏛️", Color = Color.FromArgb("#57534E") },
        new() { Id = "legislative", Name = "Legislativos", Icon = "⚖️", Color = Color.FromArgb("#44403C") },
        new() { Id = "shop", Name = "Compras", Icon = "🛍️", Color = Color.FromArgb("#BE185D") },
    };

    ChannelService()
    {
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("AplicacionTV/1.0");
    }

    public async Task<List<Channel>> GetChannelsAsync(Category category, bool forceRefresh = false)
    {
        EnsureFavoritesLoaded();

        List<Channel> list = category.Kind switch
        {
            CategoryKind.Favorites => _favorites.Values.OrderBy(c => c.Name).ToList(),
            CategoryKind.Football => (await LoadPlaylistAsync("categories/sports.m3u", forceRefresh))
                                        .Where(IsFootball).ToList(),
            CategoryKind.Spanish => await LoadPlaylistAsync("languages/spa.m3u", forceRefresh),
            _ => await LoadPlaylistAsync($"categories/{category.Id}.m3u", forceRefresh),
        };

        foreach (var c in list) c.IsFavorite = _favorites.ContainsKey(c.Url);
        return list;
    }

    static bool IsFootball(Channel c)
    {
        var text = $" {c.Name} {c.TvgId} ".ToLowerInvariant();
        return FootballKeywords.Any(k => text.Contains(k));
    }

    async Task<List<Channel>> LoadPlaylistAsync(string path, bool forceRefresh)
    {
        if (!forceRefresh && _memory.TryGetValue(path, out var cached)) return cached;

        var cacheFile = Path.Combine(FileSystem.CacheDirectory, path.Replace('/', '_'));
        string? content = null;

        if (!forceRefresh && File.Exists(cacheFile) &&
            DateTime.UtcNow - File.GetLastWriteTimeUtc(cacheFile) < CacheLifetime)
        {
            content = await File.ReadAllTextAsync(cacheFile);
        }

        if (content == null)
        {
            try
            {
                content = await _http.GetStringAsync(BaseUrl + path);
                await File.WriteAllTextAsync(cacheFile, content);
            }
            catch when (File.Exists(cacheFile))
            {
                // Sin internet: usamos la última copia descargada aunque esté vencida.
                content = await File.ReadAllTextAsync(cacheFile);
            }
        }

        var channels = M3uParser.Parse(content)
            .GroupBy(c => c.Url).Select(g => g.First())
            .OrderBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        _memory[path] = channels;
        return channels;
    }

    // ---------- Favoritos (archivo .m3u local, sin base de datos) ----------

    static string FavoritesFile => Path.Combine(FileSystem.AppDataDirectory, "favoritos.m3u");

    void EnsureFavoritesLoaded()
    {
        if (_favoritesLoaded) return;
        _favoritesLoaded = true;
        try
        {
            if (File.Exists(FavoritesFile))
                foreach (var c in M3uParser.Parse(File.ReadAllText(FavoritesFile)))
                {
                    c.IsFavorite = true;
                    _favorites[c.Url] = c;
                }
        }
        catch { /* archivo dañado: empezamos de cero */ }
    }

    public void ToggleFavorite(Channel channel)
    {
        EnsureFavoritesLoaded();
        if (_favorites.Remove(channel.Url)) channel.IsFavorite = false;
        else { _favorites[channel.Url] = channel; channel.IsFavorite = true; }
        SaveFavorites();
    }

    void SaveFavorites()
    {
        var sb = new StringBuilder("#EXTM3U\n");
        foreach (var c in _favorites.Values)
        {
            sb.Append($"#EXTINF:-1 tvg-id=\"{c.TvgId}\" tvg-logo=\"{c.Logo}\" group-title=\"{c.Group}\"");
            if (!string.IsNullOrEmpty(c.UserAgent)) sb.Append($" http-user-agent=\"{c.UserAgent}\"");
            if (!string.IsNullOrEmpty(c.Referrer)) sb.Append($" http-referrer=\"{c.Referrer}\"");
            sb.Append(',').Append(c.Name).Append('\n').Append(c.Url).Append('\n');
        }
        File.WriteAllText(FavoritesFile, sb.ToString());
    }
}
