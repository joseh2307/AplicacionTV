using System.Globalization;

namespace AplicacionTV.Models;

public static class Countries
{
    static readonly Dictionary<string, string> Spanish = new(StringComparer.OrdinalIgnoreCase)
    {
        ["AD"] = "Andorra", ["AE"] = "Emiratos Árabes", ["AF"] = "Afganistán", ["AL"] = "Albania",
        ["AM"] = "Armenia", ["AO"] = "Angola", ["AR"] = "Argentina", ["AT"] = "Austria", ["AU"] = "Australia",
        ["AW"] = "Aruba", ["AZ"] = "Azerbaiyán", ["BA"] = "Bosnia", ["BD"] = "Bangladés", ["BE"] = "Bélgica",
        ["BG"] = "Bulgaria", ["BH"] = "Baréin", ["BO"] = "Bolivia", ["BR"] = "Brasil", ["BY"] = "Bielorrusia",
        ["CA"] = "Canadá", ["CH"] = "Suiza", ["CL"] = "Chile", ["CM"] = "Camerún", ["CN"] = "China",
        ["CO"] = "Colombia", ["CR"] = "Costa Rica", ["CU"] = "Cuba", ["CW"] = "Curazao", ["CY"] = "Chipre",
        ["CZ"] = "Chequia", ["DE"] = "Alemania", ["DK"] = "Dinamarca", ["DO"] = "Rep. Dominicana",
        ["DZ"] = "Argelia", ["EC"] = "Ecuador", ["EE"] = "Estonia", ["EG"] = "Egipto", ["ES"] = "España",
        ["ET"] = "Etiopía", ["FI"] = "Finlandia", ["FR"] = "Francia", ["GB"] = "Reino Unido", ["UK"] = "Reino Unido",
        ["GE"] = "Georgia", ["GH"] = "Ghana", ["GQ"] = "Guinea Ecuatorial", ["GR"] = "Grecia", ["GT"] = "Guatemala",
        ["HK"] = "Hong Kong", ["HN"] = "Honduras", ["HR"] = "Croacia", ["HT"] = "Haití", ["HU"] = "Hungría",
        ["ID"] = "Indonesia", ["IE"] = "Irlanda", ["IL"] = "Israel", ["IN"] = "India", ["IQ"] = "Irak",
        ["IR"] = "Irán", ["IS"] = "Islandia", ["IT"] = "Italia", ["JM"] = "Jamaica", ["JO"] = "Jordania",
        ["JP"] = "Japón", ["KE"] = "Kenia", ["KR"] = "Corea del Sur", ["KW"] = "Kuwait", ["KZ"] = "Kazajistán",
        ["LB"] = "Líbano", ["LT"] = "Lituania", ["LU"] = "Luxemburgo", ["LV"] = "Letonia", ["LY"] = "Libia",
        ["MA"] = "Marruecos", ["MD"] = "Moldavia", ["ME"] = "Montenegro", ["MK"] = "Macedonia del Norte",
        ["MT"] = "Malta", ["MX"] = "México", ["MY"] = "Malasia", ["NG"] = "Nigeria", ["NI"] = "Nicaragua",
        ["NL"] = "Países Bajos", ["NO"] = "Noruega", ["NZ"] = "Nueva Zelanda", ["OM"] = "Omán",
        ["PA"] = "Panamá", ["PE"] = "Perú", ["PH"] = "Filipinas", ["PK"] = "Pakistán", ["PL"] = "Polonia",
        ["PR"] = "Puerto Rico", ["PS"] = "Palestina", ["PT"] = "Portugal", ["PY"] = "Paraguay", ["QA"] = "Catar",
        ["RO"] = "Rumania", ["RS"] = "Serbia", ["RU"] = "Rusia", ["SA"] = "Arabia Saudita", ["SE"] = "Suecia",
        ["SG"] = "Singapur", ["SI"] = "Eslovenia", ["SK"] = "Eslovaquia", ["SN"] = "Senegal", ["SV"] = "El Salvador",
        ["SY"] = "Siria", ["TH"] = "Tailandia", ["TN"] = "Túnez", ["TR"] = "Turquía", ["TT"] = "Trinidad y Tobago",
        ["TW"] = "Taiwán", ["UA"] = "Ucrania", ["US"] = "Estados Unidos", ["UY"] = "Uruguay", ["UZ"] = "Uzbekistán",
        ["VE"] = "Venezuela", ["VN"] = "Vietnam", ["XK"] = "Kosovo", ["YE"] = "Yemen", ["ZA"] = "Sudáfrica",
    };

    static readonly Dictionary<string, string> Groups = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Sports"] = "Deportes", ["Movies"] = "Películas", ["Series"] = "Series", ["News"] = "Noticias",
        ["Entertainment"] = "Entretenimiento", ["Kids"] = "Infantil", ["Animation"] = "Animación",
        ["Music"] = "Música", ["Documentary"] = "Documentales", ["Comedy"] = "Comedia", ["General"] = "General",
        ["Family"] = "Familia", ["Education"] = "Educación", ["Science"] = "Ciencia", ["Cooking"] = "Cocina",
        ["Travel"] = "Viajes", ["Lifestyle"] = "Estilo de vida", ["Culture"] = "Cultura", ["Classic"] = "Clásicos",
        ["Auto"] = "Motor", ["Outdoor"] = "Aire libre", ["Business"] = "Negocios", ["Weather"] = "Clima",
        ["Relax"] = "Relajación", ["Religious"] = "Religión", ["Public"] = "Públicos", ["Legislative"] = "Legislativos",
        ["Shop"] = "Compras", ["Interactive"] = "Interactivo", ["Undefined"] = "Otros",
    };

    public static string GetName(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return "Internacional";
        if (Spanish.TryGetValue(code, out var name)) return name;
        try { return new RegionInfo(code).EnglishName; }
        catch { return code.ToUpperInvariant(); }
    }

    /// <summary>Traduce "Movies;Series" → "Películas, Series".</summary>
    public static string TranslateGroup(string group) =>
        string.Join(", ", group.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(g => Groups.TryGetValue(g, out var es) ? es : g));

    /// <summary>Convierte un código ISO de 2 letras en su emoji de bandera.</summary>
    public static string GetFlag(string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != 2) return "🌐";
        var c = code.ToUpperInvariant();
        if (c == "UK") c = "GB";
        if (c[0] < 'A' || c[0] > 'Z' || c[1] < 'A' || c[1] > 'Z') return "🌐";
        return char.ConvertFromUtf32(0x1F1E6 + (c[0] - 'A')) + char.ConvertFromUtf32(0x1F1E6 + (c[1] - 'A'));
    }
}
