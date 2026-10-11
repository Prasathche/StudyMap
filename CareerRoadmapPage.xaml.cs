using StudyMap.Models;
using StudyMap.ViewModels;

namespace StudyMap;

[QueryProperty(nameof(CareerId), "careerId")]
public partial class CareerRoadmapPage : ContentPage
{
    private readonly CareerRoadmapViewModel _viewModel = new CareerRoadmapViewModel();
    private string _careerId = string.Empty;
    private string _loadedCareerId = string.Empty;

    public CareerRoadmapPage()
    {
        InitializeComponent();
        BindingContext = _viewModel;
        _viewModel.NavigateToDetailsAction = async stage =>
        {
            if (stage is null)
            {
                return;
            }

            await Shell.Current.Navigation.PushAsync(new CareerStageDetailsPage(stage));
        };

        _viewModel.ShowMessageAction = async message =>
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            await DisplayAlert("Saved", message, "OK");
        };
    }

    public string CareerId
    {
        get => _careerId;
        set
        {
            _careerId = Uri.UnescapeDataString(value ?? string.Empty);
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!string.IsNullOrWhiteSpace(_careerId) &&
            !string.Equals(_loadedCareerId, _careerId, StringComparison.OrdinalIgnoreCase))
        {
            await SelectCareerFromQueryAsync(_careerId);
        }
    }

    private async Task SelectCareerFromQueryAsync(string careerId)
    {
        // CareerRoadmapViewModel loads the full career catalogue asynchronously
        // when the page is constructed. Wait for that catalogue before selecting
        // the requested career so the Picker and timeline use the same CareerRoadmap.
        const int maxAttempts = 50;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var roadmap = _viewModel.AvailableRoadmaps.FirstOrDefault(
                item => string.Equals(item.CareerId, careerId, StringComparison.OrdinalIgnoreCase));

            if (roadmap is not null)
            {
                _viewModel.SelectedRoadmap = roadmap;
                _loadedCareerId = careerId;
                return;
            }

            await Task.Delay(100);
        }

        // Keep the page usable if the requested id is invalid or the data could
        // not be loaded. The normal career picker remains available.
        _loadedCareerId = string.Empty;
    }

    private async void OnHomeTapped(object? sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(MyCareerPage));
    }

}
