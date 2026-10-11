using StudyMap.Models;
using StudyMap.ViewModels;

namespace StudyMap;

[QueryProperty(nameof(CareerId), "careerId")]
public partial class CareerRoadmapPage : ContentPage
{
    private readonly CareerRoadmapViewModel _viewModel = new CareerRoadmapViewModel();
    private string _careerId = string.Empty;
    private string _loadedCareerId = string.Empty;
    private bool _settingCareerText;
    private bool _keepSuggestionsOpen;

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
        const int maxAttempts = 50;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var roadmap = _viewModel.AvailableRoadmaps.FirstOrDefault(
                item => string.Equals(item.CareerId, careerId, StringComparison.OrdinalIgnoreCase));

            if (roadmap is not null)
            {
                _viewModel.SelectedRoadmap = roadmap;
                SetCareerSearchText(roadmap.CareerName);
                _loadedCareerId = careerId;
                return;
            }

            await Task.Delay(100);
        }

        _loadedCareerId = string.Empty;
    }

    private void OnCareerSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_settingCareerText)
        {
            return;
        }

        var text = e.NewTextValue?.Trim() ?? string.Empty;
        var careers = _viewModel.AvailableRoadmaps.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(text))
        {
            careers = careers
                .Where(career => career.CareerName.Contains(text, StringComparison.OrdinalIgnoreCase))
                .OrderBy(career => career.CareerName.StartsWith(text, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(career => career.CareerName);
        }
        else
        {
            careers = careers.OrderBy(career => career.CareerName);
        }

        CareerSuggestionsView.ItemsSource = careers.Take(8).ToList();
        CareerSuggestionsBorder.IsVisible = CareerSearchEntry.IsFocused && careers.Any();
    }

    private void OnCareerSearchFocused(object? sender, FocusEventArgs e)
    {
        ShowCareerSuggestions();
    }

    private async void OnCareerSearchUnfocused(object? sender, FocusEventArgs e)
    {
        // Give the suggestion row time to receive the tap before hiding it.
        await Task.Delay(150);
        if (!_keepSuggestionsOpen)
        {
            CareerSuggestionsBorder.IsVisible = false;
        }
        _keepSuggestionsOpen = false;
    }

    private void ShowCareerSuggestions()
    {
        var text = CareerSearchEntry.Text?.Trim() ?? string.Empty;
        var careers = _viewModel.AvailableRoadmaps.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(text))
        {
            careers = careers
                .Where(career => career.CareerName.Contains(text, StringComparison.OrdinalIgnoreCase))
                .OrderBy(career => career.CareerName.StartsWith(text, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(career => career.CareerName);
        }
        else
        {
            careers = careers.OrderBy(career => career.CareerName);
        }

        CareerSuggestionsView.ItemsSource = careers.Take(8).ToList();
        CareerSuggestionsBorder.IsVisible = careers.Any();
    }

    private void OnCareerSuggestionSelected(object? sender, SelectionChangedEventArgs e)
    {
        var selected = e.CurrentSelection.FirstOrDefault() as CareerRoadmap;
        if (selected is null)
        {
            return;
        }

        _keepSuggestionsOpen = true;
        _viewModel.SelectedRoadmap = selected;
        _loadedCareerId = selected.CareerId;
        SetCareerSearchText(selected.CareerName);
        CareerSuggestionsBorder.IsVisible = false;
        CareerSuggestionsView.SelectedItem = null;
        CareerSearchEntry.Unfocus();
    }

    private void OnClearCareerClicked(object? sender, EventArgs e)
    {
        _viewModel.ClearCareerCommand.Execute(null);
        SetCareerSearchText(string.Empty);
        CareerSuggestionsBorder.IsVisible = false;
        _loadedCareerId = string.Empty;
    }

    private void SetCareerSearchText(string text)
    {
        _settingCareerText = true;
        CareerSearchEntry.Text = text;
        _settingCareerText = false;
    }

    private async void OnHomeTapped(object? sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(MyCareerPage));
    }

}
