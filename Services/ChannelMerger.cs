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

    [GeneratedRegex(@"\([^)]*\)|\[[^\]]*\]")]
    private static partial Regex TagsRegex();

    public static bool IsSpanish(Channel c)
    {
        var langs = c.Language.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (langs.Any(l => l.Equals("spa", StringComparison.OrdinalIgnoreCase))) return true;
        if (langs.Length > 0) return false; // declara otro idioma
        var name = c.Name.ToLowerInvariant();
        if (name.Contains("english")) return false;
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
            if (filter && !IsSpanish(c)) return;
            var keys = KeysOf(c).ToList();
            if (keys.Any(seen.Contains)) return;
            foreach (var k in keys) seen.Add(k);
            result.Add(c);
        }

        foreach (var c in primary) Add(c, filter: false);
        foreach (var list in extras)
            foreach (var c in list) Add(c, filter: true);

        return result;
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
