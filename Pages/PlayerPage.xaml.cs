using AplicacionTV.Models;
using AplicacionTV.Services;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;

namespace AplicacionTV.Pages;

public partial class PlayerPage : ContentPage
{
    // Si en este tiempo el canal no empieza a reproducirse, se considera caído.
    static readonly TimeSpan StartTimeout = TimeSpan.FromSeconds(30);

    /// <summary>Reproductor visible en este momento (para el manejo global de errores).</summary>
    public static PlayerPage? Current { get; private set; }

    readonly Channel _channel;
    CancellationTokenSource? _timeoutCts;
    bool _failed;
    bool _closing;

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
        Buffering.IsRunning = true;
        Buffering.IsVisible = true;
        Player.MetadataTitle = _channel.Name;
        Player.Source = MediaSource.FromUri(_channel.Url);
        StartTimeoutWatch();
    }

    void StartTimeoutWatch()
    {
        _timeoutCts?.Cancel();
        _timeoutCts = new CancellationTokenSource();
        var token = _timeoutCts.Token;
        Dispatcher.DispatchDelayed(StartTimeout, () =>
        {
            if (!token.IsCancellationRequested) ReportFailure();
        });
    }

    void OnStateChanged(object? sender, MediaStateChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // El spinner se ve desde que se toca el canal hasta que empieza la imagen.
            var busy = !_failed && e.NewState is not (MediaElementState.Playing or MediaElementState.Paused);
            Buffering.IsRunning = busy;
            Buffering.IsVisible = busy;
            if (e.NewState == MediaElementState.Playing) _timeoutCts?.Cancel();
            else if (e.NewState == MediaElementState.Failed) ReportFailure();
        });
    }

    void OnMediaFailed(object? sender, MediaFailedEventArgs e) => ReportFailure();

    // ---------- Ventana flotante (Picture-in-Picture) ----------

    public bool IsPlaying => Player.CurrentState == MediaElementState.Playing;

    /// <summary>En la ventana flotante solo se ve el video, sin barras ni controles.</summary>
    public void SetPipMode(bool pip)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            InfoBar.IsVisible = !pip;
            NavigationPage.SetHasNavigationBar(this, !pip);
            Player.ShouldShowPlaybackControls = !pip;
        });
    }

    /// <summary>El usuario cerró la ventana flotante con la "X": se detiene el canal.</summary>
    public void CloseFromPip()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (_closing) return;
            _timeoutCts?.Cancel();
            try { Player.Stop(); } catch { }
            SetPipMode(false);
            try
            {
                if (Navigation.NavigationStack.LastOrDefault() == this)
                    await Navigation.PopAsync(false);
            }
            catch { }
        });
    }

    /// <summary>
    /// El canal no se pudo reproducir: avisa al usuario y regresa a la lista de canales
    /// en lugar de dejar que la app se cierre.
    /// </summary>
    public void ReportFailure()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (_failed || _closing) return;
            _failed = true;
            _timeoutCts?.Cancel();
            try { Player.Stop(); } catch { }
            Buffering.IsRunning = false;
            Buffering.IsVisible = false;
            SetLandscape(false);
            try
            {
                await DisplayAlertAsync("Canal no disponible",
                    $"\"{_channel.Name}\" no está disponible en este momento. Prueba con otro canal.", "Aceptar");
                if (Navigation.NavigationStack.LastOrDefault() == this)
                    await Navigation.PopAsync();
            }
            catch { }
        });
    }

    void OnFavoriteTapped(object? sender, TappedEventArgs e)
    {
        ChannelService.Instance.ToggleFavorite(_channel);
        UpdateFavorite();
    }

    void UpdateFavorite() => FavLabel.Text = _channel.FavoriteIcon;

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Current = this;
#if ANDROID
        MainActivity.SetPipEnabled(true);
#endif
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _closing = true;
        if (Current == this) Current = null;
#if ANDROID
        MainActivity.SetPipEnabled(false);
#endif
        _timeoutCts?.Cancel();
        SetLandscape(false);
        try
        {
            Player.Stop();
            Player.Handler?.DisconnectHandler();
        }
        catch { }
    }

    // ---------- Pantalla completa en horizontal ----------

    void OnPlayerHandlerChanged(object? sender, EventArgs e)
    {
#if ANDROID
        if (Player.Handler?.PlatformView is Android.Views.View root &&
            FindPlayerView(root) is AndroidX.Media3.UI.PlayerView playerView)
        {
            // Se suma al manejador del toolkit (que oculta las barras del sistema).
            playerView.FullscreenButtonClick += (_, args) => SetLandscape(args.P0); // P0 = isFullScreen
        }
#elif IOS
        // Ventana flotante en iPhone: al salir de la app el video sigue en un recuadro
        // (lo maneja iOS: se puede mover, cambiar de tamaño y cerrar).
        if (Player.Handler?.PlatformView is UIKit.UIView root &&
            FindPlayerController(root) is AVKit.AVPlayerViewController controller)
        {
            controller.AllowsPictureInPicturePlayback = true;
            controller.CanStartPictureInPictureAutomaticallyFromInline = true;
        }
#endif
    }

#if IOS
    static AVKit.AVPlayerViewController? FindPlayerController(UIKit.UIView view)
    {
        if (view.NextResponder is AVKit.AVPlayerViewController vc) return vc;
        foreach (var child in view.Subviews)
            if (FindPlayerController(child) is { } found) return found;
        return null;
    }
#endif

#if ANDROID
    static AndroidX.Media3.UI.PlayerView? FindPlayerView(Android.Views.View view)
    {
        if (view is AndroidX.Media3.UI.PlayerView pv) return pv;
        if (view is Android.Views.ViewGroup group)
            for (int i = 0; i < group.ChildCount; i++)
                if (group.GetChildAt(i) is { } child && FindPlayerView(child) is { } found)
                    return found;
        return null;
    }
#endif

    void SetLandscape(bool landscape)
    {
#if ANDROID
        var activity = Platform.CurrentActivity;
        if (activity == null) return;
        activity.RequestedOrientation = landscape
            ? Android.Content.PM.ScreenOrientation.SensorLandscape
            : Android.Content.PM.ScreenOrientation.Unspecified;
#endif
        InfoBar.IsVisible = !landscape;
        NavigationPage.SetHasNavigationBar(this, !landscape);
    }
}
