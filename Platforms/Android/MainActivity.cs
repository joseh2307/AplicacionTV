using Android.App;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;
using Android.Util;
using AplicacionTV.Pages;

namespace AplicacionTV;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
	SupportsPictureInPicture = true, ResizeableActivity = true,
	ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
	// Al ir a otra app (botón de inicio o de recientes) mientras se ve un canal,
	// la app se minimiza a una ventana flotante (Picture-in-Picture).
	// El sistema permite agrandarla/achicarla con los dedos y cerrarla con su "X".
	protected override void OnUserLeaveHint()
	{
		base.OnUserLeaveHint();
		if (!OperatingSystem.IsAndroidVersionAtLeast(26)) return;
		if (PlayerPage.Current is not { IsPlaying: true }) return;
		try
		{
			var pipParams = new PictureInPictureParams.Builder()
				.SetAspectRatio(new Rational(16, 9))!
				.Build();
			EnterPictureInPictureMode(pipParams!);
		}
		catch { /* el dispositivo no soporta la ventana flotante */ }
	}

	public override void OnPictureInPictureModeChanged(bool isInPictureInPictureMode, Configuration? newConfig)
	{
		base.OnPictureInPictureModeChanged(isInPictureInPictureMode, newConfig);
		PlayerPage.Current?.SetPipMode(isInPictureInPictureMode);

		// Si al salir de la ventana flotante la app quedó detenida, el usuario la cerró con la "X".
		if (!isInPictureInPictureMode && Lifecycle.CurrentState == AndroidX.Lifecycle.Lifecycle.State.Created)
			PlayerPage.Current?.CloseFromPip();
	}

	protected override void OnStop()
	{
		base.OnStop();
		// Detener la actividad mientras está en ventana flotante = la ventana se cerró.
		if (OperatingSystem.IsAndroidVersionAtLeast(24) && IsInPictureInPictureMode)
			PlayerPage.Current?.CloseFromPip();
	}
}
