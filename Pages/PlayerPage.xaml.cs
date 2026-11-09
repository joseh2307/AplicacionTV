using AplicacionTV.Models;
using AplicacionTV.Services;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;

namespace AplicacionTV.Pages;

public partial class PlayerPage : ContentPage
{
    readonly Channel _channel;

    public PlayerPage(Channel channel)
    {
        InitializeComponent();
        _channel = channel;
        Title = channel.Name;
        NameLabel.Text = channel.Name;
        InfoLabel.Text = channel.Subtitle;
        UpdateFavorite();
        Play();
    }

    void Play()
    {
        ErrorPanel.IsVisible = false;
        Buffering.IsRunning = true;
        Buffering.IsVisible = true;
        Player.Source = MediaSource.FromUri(_channel.Url);
        Player.MetadataTitle = _channel.Name;
        Player.MetadataArtworkUrl = _channel.Logo;
    }

    void OnStateChanged(object? sender, MediaStateChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var busy = e.NewState is MediaElementState.Opening or MediaElementState.Buffering;
            Buffering.IsRunning = busy;
            Buffering.IsVisible = busy;
        });
    }

    void OnMediaFailed(object? sender, MediaFailedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Buffering.IsRunning = false;
            Buffering.IsVisible = false;
            ErrorPanel.IsVisible = true;
        });
    }

    void OnRetryClicked(object? sender, EventArgs e)
    {
        Player.Stop();
        Player.Source = null;
        Play();
    }

    async void OnOpenExternalClicked(object? sender, EventArgs e)
    {
        try
        {
#if ANDROID
            var intent = new Android.Content.Intent(Android.Content.Intent.ActionView);
            intent.SetDataAndType(Android.Net.Uri.Parse(_channel.Url), "video/*");
            intent.AddFlags(Android.Content.ActivityFlags.NewTask);
            var chooser = Android.Content.Intent.CreateChooser(intent, "Abrir con");
            chooser!.AddFlags(Android.Content.ActivityFlags.NewTask);
            Platform.AppContext.StartActivity(chooser);
#else
            await Launcher.Default.OpenAsync(new Uri(_channel.Url));
#endif
        }
        catch
        {
            await DisplayAlertAsync("Sin reproductor", "No hay otra app para abrir este canal.", "OK");
        }
    }

    void OnFavoriteTapped(object? sender, TappedEventArgs e)
    {
        ChannelService.Instance.ToggleFavorite(_channel);
        UpdateFavorite();
    }

    void UpdateFavorite() => FavLabel.Text = _channel.FavoriteIcon;

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        Player.Stop();
        Player.Handler?.DisconnectHandler();
    }
}
