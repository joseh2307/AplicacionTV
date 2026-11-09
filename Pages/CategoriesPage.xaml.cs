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

    async void OnCategoryTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is Category category)
            await Navigation.PushAsync(new ChannelsPage(category));
    }
}
