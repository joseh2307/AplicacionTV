using Android.App;
using Android.Runtime;
using AplicacionTV.Pages;

namespace AplicacionTV;

[Application]
public class MainApplication : MauiApplication
{
	public MainApplication(IntPtr handle, JniHandleOwnership ownership)
		: base(handle, ownership)
	{
	}

	protected override MauiApp CreateMauiApp()
	{
		// Si un canal provoca un error mientras se reproduce, no cerramos la app:
		// se avisa al usuario y se regresa a la lista de canales.
		AndroidEnvironment.UnhandledExceptionRaiser += (_, e) =>
		{
			if (PlayerPage.Current is { } player)
			{
				e.Handled = true;
				player.ReportFailure();
			}
		};
		TaskScheduler.UnobservedTaskException += (_, e) =>
		{
			e.SetObserved();
			PlayerPage.Current?.ReportFailure();
		};
		return MauiProgram.CreateMauiApp();
	}
}
