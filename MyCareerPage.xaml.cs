using System.Text.Json;
using StudyMap.Models;
using StudyMap.Services;

namespace StudyMap;

public partial class MyCareerPage : ContentPage
{
    private readonly FavoritesService _favoritesService = new();

    private List<Career> _allCareers = [];

    public MyCareerPage()
    {
        InitializeComponent();

        CareerCountLabel.Text = "0";
        
       
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadSavedCareersAsync();
    }

    private async Task LoadSavedCareersAsync()
    {
        try
        {
            // Load careers from careers.json
            await using var stream =
                await FileSystem.OpenAppPackageFileAsync("careers.json");

            _allCareers =
                await JsonSerializer.DeserializeAsync<List<Career>>(
                    stream,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    })
                ?? [];

            // Get favourite career IDs
            var favoriteIds = _favoritesService.GetAll();

            // Find matching career objects
            var savedCareers = _allCareers
                .Where(career => favoriteIds.Contains(career.Id))
                .ToList();

            // Update count
            CareerCountLabel.Text = savedCareers.Count.ToString();

    // Roadmaps are not implemented yet
RoadmapCountLabel.Text = "0";

            // Update collection
            SavedCareersCollection.ItemsSource = savedCareers;

            // Show empty state only when there are no favorites
            SavedCareersCollection.IsVisible = savedCareers.Count > 0;
            SavedCareersEmptyState.IsVisible = savedCareers.Count == 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[MyCareerPage] Failed to load saved careers: {ex}");
        }
    }

    private async void OnSavedCareerTapped(
        object? sender,
        TappedEventArgs e)
    {
        if (e.Parameter is not Career career)
            return;

        await Shell.Current.GoToAsync(
            $"{nameof(CareerDetailPage)}?careerId={Uri.EscapeDataString(career.Id)}");
    }

    private async void OnBackClicked(
        object? sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private void OnCareersTabTapped(
        object? sender,
        TappedEventArgs e)
    {
        SavedCareersSection.IsVisible = true;
        RoadmapsSection.IsVisible = false;

        CareersTab.BackgroundColor =
            Color.FromArgb("#5B3FD3");

        RoadmapsTab.BackgroundColor =
            Colors.White;

        SetTabTextColor(
            CareersTab,
            Colors.White);

        SetTabTextColor(
            RoadmapsTab,
            Color.FromArgb("#555555"));
    }
private async void OnRemoveSavedCareerClicked(
    object? sender,
    EventArgs e)
{
    if (sender is not Button button)
        return;

    if (button.CommandParameter is not Career career)
        return;

    bool confirm = await DisplayAlert(
        "Remove Career",
        $"Remove \"{career.Name}\" from your saved careers?",
        "Remove",
        "Cancel");

    if (!confirm)
        return;

    _favoritesService.Remove(career.Id);

    await LoadSavedCareersAsync();
}
    private void OnRoadmapsTabTapped(
        object? sender,
        TappedEventArgs e)
    {
        SavedCareersSection.IsVisible = false;
        RoadmapsSection.IsVisible = true;

        RoadmapsTab.BackgroundColor =
            Color.FromArgb("#5B3FD3");

        CareersTab.BackgroundColor =
            Colors.White;

        SetTabTextColor(
            RoadmapsTab,
            Colors.White);

        SetTabTextColor(
            CareersTab,
            Color.FromArgb("#555555"));
    }

    private static void SetTabTextColor(
        Border border,
        Color color)
    {
        if (border.Content is Label label)
        {
            label.TextColor = color;
        }
    }

    private async void OnExploreClicked(
        object? sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync(
            "//Explore");
    }
}