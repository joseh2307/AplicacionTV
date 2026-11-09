using AplicacionTV.Models;
using AplicacionTV.Services;

namespace AplicacionTV.Pages;

public partial class ChannelsPage : ContentPage
{
    const string AllCountries = "🌐 Todos los países";

    readonly Category _category;
    List<Channel> _all = new();
    List<string> _countryCodes = new();
    bool _loaded;

    public ChannelsPage(Category category)
    {
        InitializeComponent();
        _category = category;
        Title = $"{category.Icon} {category.Name}";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!_loaded || _category.Kind == CategoryKind.Favorites)
            await LoadAsync(false);
    }

    async Task LoadAsync(bool force)
    {
        Loading.IsRunning = true;
        EmptyLabel.Text = "Cargando canales...";
        try
        {
            _all = await ChannelService.Instance.GetChannelsAsync(_category, force);
            _loaded = true;
            BuildCountryList();
            ApplyFilter();
            if (_all.Count == 0)
                EmptyLabel.Text = _category.Kind == CategoryKind.Favorites
                    ? "Aún no tienes favoritos.\nToca ☆ junto a un canal para agregarlo."
                    : "No se encontraron canales.";
        }
        catch (Exception)
        {
            EmptyLabel.Text = "No se pudo descargar la lista.\nRevisa tu conexión a internet y desliza hacia abajo para reintentar.";
        }
        finally
        {
            Loading.IsRunning = false;
            Refresh.IsRefreshing = false;
        }
    }

    void BuildCountryList()
    {
        var previous = CountryPicker.SelectedIndex > 0 ? _countryCodes[CountryPicker.SelectedIndex] : null;

        _countryCodes = _all.Select(c => c.CountryCode).Distinct()
            .OrderBy(Countries.GetName, StringComparer.CurrentCultureIgnoreCase).ToList();
        _countryCodes.Insert(0, "*");

        CountryPicker.SelectedIndexChanged -= OnFilterChanged;
        CountryPicker.ItemsSource = _countryCodes
            .Select(code => code == "*" ? AllCountries : $"{Countries.GetFlag(code)} {Countries.GetName(code)}")
            .ToList();
        var idx = previous != null ? _countryCodes.IndexOf(previous) : 0;
        CountryPicker.SelectedIndex = idx < 0 ? 0 : idx;
        CountryPicker.SelectedIndexChanged += OnFilterChanged;
    }

    void ApplyFilter()
    {
        IEnumerable<Channel> query = _all;

        var text = Search.Text?.Trim();
        if (!string.IsNullOrEmpty(text))
            query = query.Where(c => c.Name.Contains(text, StringComparison.CurrentCultureIgnoreCase)
                                  || c.CountryName.Contains(text, StringComparison.CurrentCultureIgnoreCase));

        if (CountryPicker.SelectedIndex > 0)
        {
            var code = _countryCodes[CountryPicker.SelectedIndex];
            query = query.Where(c => c.CountryCode == code);
        }

        var result = query.ToList();
        ChannelsView.ItemsSource = result;
        CountLabel.Text = $"{result.Count} canales";
        if (result.Count == 0 && _all.Count > 0) EmptyLabel.Text = "Ningún canal coincide con el filtro.";
    }

    void OnFilterChanged(object? sender, EventArgs e) => ApplyFilter();

    async void OnRefreshing(object? sender, EventArgs e) => await LoadAsync(true);

    async void OnChannelTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is Channel channel)
            await Navigation.PushAsync(new PlayerPage(channel));
    }

    void OnFavoriteTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not Channel channel) return;
        ChannelService.Instance.ToggleFavorite(channel);
        if (_category.Kind == CategoryKind.Favorites && !channel.IsFavorite)
        {
            _all.Remove(channel);
            ApplyFilter();
        }
    }
}
