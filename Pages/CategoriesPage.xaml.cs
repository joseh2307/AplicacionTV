using AplicacionTV.Models;
using AplicacionTV.Services;

namespace AplicacionTV.Pages;

public partial class CategoriesPage : ContentPage
{
    public CategoriesPage()
    {
        InitializeComponent();
        CategoriesView.ItemsSource = ChannelService.Instance.Categories;
    }

    // Sale de la app directamente, sin pedir confirmación.
    void OnExitClicked(object? sender, EventArgs e)
    {
#if ANDROID
        Platform.CurrentActivity?.FinishAndRemoveTask();
        Java.Lang.JavaSystem.Exit(0);
#else
        Application.Current?.Quit();
#endif
    }

    async void OnCategoryTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is Category category)
            await Navigation.PushAsync(new ChannelsPage(category));
    }
}
