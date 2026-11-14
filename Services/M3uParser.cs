using System.Text.RegularExpressions;
using AplicacionTV.Models;

namespace AplicacionTV.Services;

/// <summary>Parser sencillo del formato M3U/M3U8 extendido que publica iptv-org.</summary>
public static partial class M3uParser
{
    [GeneratedRegex("([a-zA-Z0-9-]+)=\"([^\"]*)\"")]
    private static partial Regex AttrRegex();

    public static List<Channel> Parse(string content)
    {
        var result = new List<Channel>();
        Channel? current = null;

        foreach (var raw in content.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0) continue;

            if (line.StartsWith("#EXTINF", StringComparison.OrdinalIgnoreCase))
            {
                current = new Channel();
                var comma = FindTitleComma(line);
                var header = comma >= 0 ? line[..comma] : line;
                current.Name = comma >= 0 ? CleanName(line[(comma + 1)..]) : "Canal";

                foreach (Match m in AttrRegex().Matches(header))
                {
                    var key = m.Groups[1].Value.ToLowerInvariant();
                    var val = m.Groups[2].Value;
                    switch (key)
                    {
                        case "tvg-id": current.TvgId = val; break;
                        case "tvg-logo": current.Logo = val; break;
                        case "group-title": current.Group = val; break;
                        case "http-user-agent": current.UserAgent = val; break;
                        case "http-referrer": current.Referrer = val; break;
                        case "tvg-country": current.CountryCode = FirstCode(val); break;
                        case "tvg-language": current.Language = val; break;
                    }
                }
                if (string.IsNullOrEmpty(current.CountryCode))
                    current.CountryCode = ExtractCountry(current.TvgId);
                if (string.IsNullOrEmpty(current.CountryCode))
                    current.CountryCode = CountryFromFlag(current.Group); // p. ej. "🇨🇷 Costa Rica"
            }
            else if (line.StartsWith("#EXTVLCOPT", StringComparison.OrdinalIgnoreCase) && current != null)
            {
                var opt = line[(line.IndexOf(':') + 1)..];
                if (opt.StartsWith("http-user-agent=")) current.UserAgent = opt["http-user-agent=".Length..];
                else if (opt.StartsWith("http-referrer=")) current.Referrer = opt["http-referrer=".Length..];
            }
            else if (!line.StartsWith('#') && current != null)
            {
                current.Url = line;
                if (string.IsNullOrWhiteSpace(current.Group)) current.Group = "General";
                result.Add(current);
                current = null;
            }
        }
        return result;
    }

    // Quita marcas como "Ⓢ", "Ⓖ" o "Ⓨ" que algunas listas agregan al final del nombre.
    static string CleanName(string name)
    {
        var chars = name.Where(ch => ch < 'Ⓐ' || ch > 'ⓩ').ToArray();
        var clean = new string(chars).Trim();
        return clean.Length == 0 ? "Canal" : clean;
    }

    // Convierte una bandera emoji (dos "indicadores regionales") en su código de país.
    static string CountryFromFlag(string text)
    {
        var runes = text.EnumerateRunes().Take(2).ToArray();
        if (runes.Length == 2 && runes.All(r => r.Value is >= 0x1F1E6 and <= 0x1F1FF))
            return new string(runes.Select(r => (char)('A' + r.Value - 0x1F1E6)).ToArray());
        return "";
    }

    // La coma que separa atributos del título es la primera coma fuera de comillas.
    static int FindTitleComma(string line)
    {
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == '"') inQuotes = !inQuotes;
            else if (line[i] == ',' && !inQuotes) return i;
        }
        return -1;
    }

    // tvg-country puede venir como "CR" o "CR;US": tomamos el primero.
    static string FirstCode(string value)
    {
        var code = value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .FirstOrDefault() ?? "";
        return code.Length == 2 ? code.ToUpperInvariant() : "";
    }

    // tvg-id tiene la forma "NombreCanal.mx@SD" → "MX"
    static string ExtractCountry(string tvgId)
    {
        if (string.IsNullOrEmpty(tvgId)) return "";
        var at = tvgId.IndexOf('@');
        var id = at >= 0 ? tvgId[..at] : tvgId;
        var dot = id.LastIndexOf('.');
        if (dot < 0 || dot == id.Length - 1) return "";
        var code = id[(dot + 1)..];
        return code.Length == 2 ? code.ToUpperInvariant() : "";
    }
}
