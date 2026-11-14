using System.Text;
using System.Text.RegularExpressions;
using AplicacionTV.Models;

namespace AplicacionTV.Services;

/// <summary>
/// Une varias listas M3U dejando solo canales en español y sin repetidos.
/// Los canales de la lista principal se conservan tal cual; de las demás listas
/// solo se agregan los que todavía no existen.
/// </summary>
public static partial class ChannelMerger
{
    // Países de habla hispana: si un canal no indica idioma, se acepta si es de uno de estos.
    static readonly HashSet<string> SpanishCountries = new(StringComparer.OrdinalIgnoreCase)
    {
        "AR", "BO", "CL", "CO", "CR", "CU", "DO", "EC", "ES", "GQ", "GT", "HN",
        "MX", "NI", "PA", "PE", "PR", "PY", "SV", "UY", "VE",
    };

    // Pistas en el nombre de canales en español de países no hispanos (p. ej. EE. UU.).
    static readonly string[] SpanishNameHints =
    {
        "español", "espanol", "latino", "latin america", "latinoamerica", "latam",
        "deportes", "tudn", "univision", "telemundo", "unimas",
    };

    // Canales de países hispanos que NO emiten en español (catalán, euskera, gallego,
    // o versiones para Brasil/Europa del Este de cadenas latinas).
    static readonly string[] NotSpanishHints =
    {
        "3cat", "català", "catala", "catalunya", "taronja", "cugat", "bon dia", "terrassa", "emporda",
        "reus", "girona", "lleida", "tarragona", "valls", "badalona", "sabadell", "manresa", "osona",
        "vallès", "valles", "barcelona tv", "betevé", "beteve", "el punt", "euskal", "etb", "kanala",
        "hamaika", "galicia", "tvg", "brazil", "brasil", "portug", "bulgar", "czech", "romania",
        "hungary", "poland", "cee", "à punt", "a punt", "televisio ", "televisió", "telebista",
        "tv3", "el 33", "sx3", "el 9 tv", "olot", "penedes", "piera", "pirineus", "mataro", "tac12",
        "bergueda", "costa brava", "hospitalet", "ripolles", "vendrell", "cardedeu", "votv", "etv ",
        "durangaldeko", "urola", "biar", "storie", "saveurs", "kinokazka", "nordic", "3/24",
        " austria", " finland", " germany", " italy", " switzerland", " uk ", " de ", " it ", " fr ",
    };

    [GeneratedRegex(@"\([^)]*\)|\[[^\]]*\]")]
    private static partial Regex TagsRegex();

    public static bool IsSpanish(Channel c)
    {
        var langs = c.Language.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (langs.Any(l => l.Equals("spa", StringComparison.OrdinalIgnoreCase))) return true;
        if (langs.Length > 0) return false; // declara otro idioma
        var name = $" {TagsRegex().Replace(c.Name, " ")} ".ToLowerInvariant();
        if (name.Contains("english") || NotSpanishHints.Any(name.Contains)) return false;
        if (SpanishCountries.Contains(c.CountryCode)) return true;
        return SpanishNameHints.Any(name.Contains);
    }

    public static List<Channel> Merge(List<Channel> primary, IEnumerable<List<Channel>> extras)
    {
        var result = new List<Channel>();
        var seen = new HashSet<string>();

        void Add(Channel c, bool filter)
        {
            if (string.IsNullOrWhiteSpace(c.Url)) return;
            if (filter && (!IsSpanish(c) || !IsPlayable(c.Url))) return;
            var keys = KeysOf(c).ToList();
            if (keys.Any(seen.Contains)) return;
            foreach (var k in keys) seen.Add(k);
            if (filter) AssignCategory(c);
            result.Add(c);
        }

        foreach (var c in primary) Add(c, filter: false);
        foreach (var list in extras)
            foreach (var c in list) Add(c, filter: true);

        return result;
    }

    // Los enlaces de YouTube no son transmisiones directas y el reproductor no puede abrirlos.
    static bool IsPlayable(string url) =>
        !url.Contains("youtube.com", StringComparison.OrdinalIgnoreCase) &&
        !url.Contains("youtu.be", StringComparison.OrdinalIgnoreCase);

    // Ids de categoría de iptv-org (coinciden con los de la app).
    static readonly HashSet<string> KnownGroups = new(StringComparer.OrdinalIgnoreCase)
    {
        "auto", "animation", "business", "classic", "comedy", "cooking", "culture", "documentary",
        "education", "entertainment", "family", "general", "interactive", "kids", "legislative",
        "lifestyle", "movies", "music", "news", "outdoor", "public", "relax", "religious", "series",
        "science", "shop", "sports", "travel", "weather",
    };

    // Palabras del nombre (o del grupo) → categoría, para listas agrupadas por país.
    static readonly (string Group, string[] Words)[] CategoryHints =
    {
        ("Sports",      new[] { "deporte", "sport", "fútbol", "futbol", "espn", "tudn", "gol", "tyc", "fox sports", "bein", "dazn", "fútbol", "liga", "win sports", "tigo sports" }),
        ("News",        new[] { "noticia", "news", "24h", "24 h", "informa", "cnn", "telesur", "dw ", "france 24", "euronews", "canal 24", "notic" }),
        ("Movies",      new[] { "cine", "movie", "película", "pelicula", "film", "cinema" }),
        ("Kids",        new[] { "kids", "infantil", "niños", "ninos", "junior", "cartoon", "toon", "baby", "clan" }),
        ("Music",       new[] { "música", "musica", "music", "radio", " fm", "hits", "stereo", "estéreo", "mtv", "vh1" }),
        ("Religious",   new[] { "cristo", "iglesia", "católic", "catolic", "evangel", "enlace", "fe ", "dios", "jesús", "jesus", "esne", "ewtn", "maria", "bíblic", "biblic", "adventist", "hope" }),
        ("Series",      new[] { "series", "novela", "telenovela" }),
        ("Documentary", new[] { "document", "historia", "history", "discovery", "natura" }),
        ("Cooking",     new[] { "cocina", "cook", "gourmet", "chef" }),
        ("Comedy",      new[] { "comedia", "comedy", "humor", "risa" }),
        ("Culture",     new[] { "cultura", "culture", " arte " }),
        ("Education",   new[] { "educa", "universi", "escuela", "ucr" }),
        ("Legislative", new[] { "congreso", "senado", "asamblea", "parlament", "legislat" }),
    };

    /// <summary>
    /// Algunas listas usan el país como grupo ("Spain", "🇨🇷 Costa Rica"); en ese caso
    /// se deduce la categoría por el nombre del canal, o se deja en "General".
    /// </summary>
    static void AssignCategory(Channel c)
    {
        var parts = c.Group.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length > 0 && parts.All(KnownGroups.Contains)) return;

        var text = $" {c.Name} {c.Group} ".ToLowerInvariant();
        foreach (var (group, words) in CategoryHints)
            if (words.Any(text.Contains)) { c.Group = group; return; }
        c.Group = "General";
    }

    // Un canal se considera repetido si coincide su enlace, su tvg-id o su nombre + país.
    static IEnumerable<string> KeysOf(Channel c)
    {
        yield return "url:" + NormalizeUrl(c.Url);
        if (!string.IsNullOrWhiteSpace(c.TvgId)) yield return "id:" + c.TvgId.Trim().ToLowerInvariant();
        var name = NormalizeName(c.Name);
        if (name.Length > 0) yield return $"name:{name}|{c.CountryCode}";
    }

    static string NormalizeUrl(string url)
    {
        var u = url.Trim().ToLowerInvariant();
        var q = u.IndexOf('?');
        if (q >= 0) u = u[..q];
        if (u.StartsWith("https://")) u = u[8..];
        else if (u.StartsWith("http://")) u = u[7..];
        return u.TrimEnd('/');
    }

    static string NormalizeName(string name)
    {
        var plain = TagsRegex().Replace(name, " ").Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in plain)
            if (char.IsLetterOrDigit(ch)) sb.Append(char.ToLowerInvariant(ch));
        return sb.ToString();
    }
}
