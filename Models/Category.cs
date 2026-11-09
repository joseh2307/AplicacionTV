namespace AplicacionTV.Models;

public enum CategoryKind
{
    /// <summary>Lista de una categoría de iptv-org (categories/{id}.m3u).</summary>
    Remote,
    /// <summary>Canales deportivos filtrados por palabras clave de fútbol.</summary>
    Football,
    /// <summary>Canales marcados como favoritos (guardados en el teléfono).</summary>
    Favorites,
    /// <summary>Canales en idioma español (languages/spa.m3u).</summary>
    Spanish,
}

public class Category
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Icon { get; init; } = "";
    public Color Color { get; init; } = Colors.SlateGray;
    public CategoryKind Kind { get; init; } = CategoryKind.Remote;
}
