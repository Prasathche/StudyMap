using StudyMap.Services;
using StudyMap.ViewModels;

namespace StudyMap;

public partial class StreamSelectionPage : ContentPage
{
    private readonly StreamSelectionViewModel _viewModel;

    public StreamSelectionPage()
    {
        InitializeComponent();
        _viewModel = new StreamSelectionViewModel();
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Animate cards in on appear
        await AnimateCardsIn();
    }

    private async Task AnimateCardsIn()
    {
        var cards = new[] { scienceCard, commerceCard, artsCard, notSureCard };
        foreach (var card in cards)
        {
            card.Opacity = 0;
            card.TranslationY = 30;
        }

        var tasks = cards.Select(async (card, i) =>
        {
            await Task.Delay(i * 80);
            await Task.WhenAll(
                card.FadeTo(1, 350, Easing.CubicOut),
                card.TranslateTo(0, 0, 350, Easing.CubicOut)
            );
        });

        await Task.WhenAll(tasks);
    }

    private async void OnScienceClicked(object sender, EventArgs e)
    {
        await AnimateTap(scienceCard);
        await Shell.Current.GoToAsync(
            $"{nameof(StreamCareerListPage)}?streamName=Science");
    }

    private async void OnCommerceClicked(object sender, EventArgs e)
    {
        await AnimateTap(commerceCard);
        await Shell.Current.GoToAsync(
            $"{nameof(StreamCareerListPage)}?streamName=Commerce");
    }

    private async void OnArtsClicked(object sender, EventArgs e)
    {
        await AnimateTap(artsCard);
        await Shell.Current.GoToAsync(
            $"{nameof(StreamCareerListPage)}?streamName=Arts");
    }

    private async void OnNotSureTapped(object? sender, TappedEventArgs e)
    {
        await AnimateTap(notSureCard);
        await Shell.Current.GoToAsync($"//{nameof(CareerDiscoveryPage)}");
    }

    private static async Task AnimateTap(Border card)
    {
        await card.ScaleTo(0.97, 80, Easing.CubicIn);
        await card.ScaleTo(1.0, 120, Easing.CubicOut);
    }
}