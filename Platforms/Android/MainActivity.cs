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
	/// <summary>
	/// Activa o desactiva la ventana flotante automática. Se activa mientras se ve un canal:
	/// al ir a otra app (inicio, recientes o gesto) el video pasa a un recuadro pequeño que
	/// el sistema permite agrandar/achicar con los dedos y cerrar con su "X".
	/// </summary>
	public static void SetPipEnabled(bool enabled)
	{
		if (!OperatingSystem.IsAndroidVersionAtLeast(26)) return;
		if (Platform.CurrentActivity is not MainActivity activity) return;
		try
		{
			activity._pipEnabled = enabled;
			if (OperatingSystem.IsAndroidVersionAtLeast(31))
				activity.SetPictureInPictureParams(activity.BuildPipParams(enabled));
		}
		catch { /* el dispositivo no soporta la ventana flotante */ }
	}

	bool _pipEnabled;

	PictureInPictureParams BuildPipParams(bool autoEnter)
	{
		var builder = new PictureInPictureParams.Builder().SetAspectRatio(new Rational(16, 9))!;
		if (OperatingSystem.IsAndroidVersionAtLeast(31))
		{
			builder.SetAutoEnterEnabled(autoEnter);
			builder.SetSeamlessResizeEnabled(true);
		}
		return builder.Build()!;
	}

	// Android 8 a 11 (y respaldo en 12+): al salir de la app se pide la ventana flotante.
	protected override void OnUserLeaveHint()
	{
		base.OnUserLeaveHint();
		if (!_pipEnabled || !OperatingSystem.IsAndroidVersionAtLeast(26)) return;
		if (IsInPictureInPictureMode) return;
		try { EnterPictureInPictureMode(BuildPipParams(true)); }
		catch { /* el usuario desactivó la ventana flotante para esta app */ }
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
