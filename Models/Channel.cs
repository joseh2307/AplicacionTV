using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AplicacionTV.Models;

/// <summary>Un canal leído de una lista M3U de iptv-org.</summary>
public class Channel : INotifyPropertyChanged
{
    public string Name { get; set; } = "";
    public string Logo { get; set; } = "";
    public string Url { get; set; } = "";
    public string Group { get; set; } = "";
    public string TvgId { get; set; } = "";
    public string CountryCode { get; set; } = "";
    public string? UserAgent { get; set; }
    public string? Referrer { get; set; }

    public string CountryName => Countries.GetName(CountryCode);
    public string Flag => Countries.GetFlag(CountryCode);
    public string GroupName => Countries.TranslateGroup(Group);
    public string Subtitle => string.IsNullOrEmpty(CountryCode) ? $"🌐 {GroupName}" : $"{Flag} {CountryName} · {GroupName}";
    public bool HasLogo => !string.IsNullOrWhiteSpace(Logo);
    public string Initial => string.IsNullOrEmpty(Name) ? "?" : Name.Substring(0, 1).ToUpperInvariant();

    bool _isFavorite;
    public bool IsFavorite
    {
        get => _isFavorite;
        set { if (_isFavorite != value) { _isFavorite = value; OnPropertyChanged(); OnPropertyChanged(nameof(FavoriteIcon)); } }
    }
    public string FavoriteIcon => IsFavorite ? "★" : "☆";

    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
