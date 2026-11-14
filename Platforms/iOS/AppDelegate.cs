using AVFoundation;
using Foundation;

namespace AplicacionTV;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
	protected override MauiApp CreateMauiApp()
	{
		// Necesario para que iOS permita la ventana flotante (Picture-in-Picture).
		AVAudioSession.SharedInstance().SetCategory(AVAudioSessionCategory.Playback);
		return MauiProgram.CreateMauiApp();
	}
}
