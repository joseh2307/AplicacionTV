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
                current.Name = comma >= 0 ? line[(comma + 1)..].Trim() : "Canal";

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
                    }
                }
                current.CountryCode = ExtractCountry(current.TvgId);
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
