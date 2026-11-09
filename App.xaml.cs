using AplicacionTV.Pages;

namespace AplicacionTV;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
		UserAppTheme = AppTheme.Dark;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var nav = new NavigationPage(new CategoriesPage())
		{
			BarBackgroundColor = Color.FromArgb("#0F172A"),
			BarTextColor = Colors.White,
		};
		return new Window(nav);
	}
}
