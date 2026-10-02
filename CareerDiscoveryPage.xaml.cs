using StudyMap.Services;

namespace StudyMap;

public partial class CareerDiscoveryPage : ContentPage
{
    public CareerDiscoveryPage()
    {
        InitializeComponent();
    }

    private async void OnGetStartedClicked(object? sender, EventArgs e)
    {
        CareerDiscoverySessionService.Instance.Reset();
        await Shell.Current.GoToAsync($"{nameof(CareerQuestionPage)}?index=1");
    }

    private async void OnMaybeLaterClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//Explore");
    }
    private async void OnHomeTapped(object? sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(MyCareerPage));
    }

}
