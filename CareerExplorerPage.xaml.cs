using System.Collections.ObjectModel;
using StudyMap.Models;
using StudyMap.Services;

namespace StudyMap;

public partial class CareerExplorerPage : ContentPage
{
    private const int PageSize = 30;
    private readonly CareersService _careersService = new();
    private readonly List<Career> _allCareers = [];
    private List<Career> _filteredCareers = [];
    private bool _isLoaded;
    private string _searchText = string.Empty;
    private string _selectedCategory = "All";
    private int _visibleCount;
    private Button? _loadMoreButton;

    public ObservableCollection<Career> FilteredCareers { get; } = [];
    public ObservableCollection<Career> PopularCareers { get; } = [];

    public CareerExplorerPage()
    {
        InitializeComponent();
        BindingContext = this;
        ApplyCategoryStyles();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_isLoaded) return;
        _isLoaded = true;
        await LoadCareersAsync();
    }

    private async Task LoadCareersAsync()
    {
        try
        {
            var careers = await _careersService.GetAllCareersAsync();
            _allCareers.Clear();
            _allCareers.AddRange(careers);
            ApplyFilters();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CareerExplorer] {ex}");
            await DisplayAlert("Career Explorer", "Unable to load careers right now.", "OK");
        }
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        // TextChanged is the authoritative search event. This also fires when
        // the Entry clear button removes the text, so the full list returns.
        _searchText = e.NewTextValue?.Trim() ?? string.Empty;
        ClearSearchButton.IsVisible = !string.IsNullOrWhiteSpace(_searchText);
        ApplyFilters();
    }

    private void OnSearchCompleted(object? sender, EventArgs e)
    {
        _searchText = SearchEntry?.Text?.Trim() ?? string.Empty;
        ClearSearchButton.IsVisible = !string.IsNullOrWhiteSpace(_searchText);
        ApplyFilters();
    }

    private void OnClearSearchClicked(object? sender, EventArgs e)
    {
        SearchEntry.Text = string.Empty;
        SearchEntry.Focus();
    }

    private void OnCategoryClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button) return;
        _selectedCategory = button.Text;
        ApplyCategoryStyles();
        ApplyFilters();
    }

    private async void OnCareerTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not Career career) return;
        await Shell.Current.GoToAsync($"{nameof(CareerDetailPage)}?careerId={Uri.EscapeDataString(career.Id)}");
    }

    private async void OnHomeTapped(object? sender, TappedEventArgs e)
    {
        try { await Shell.Current.GoToAsync(nameof(MyCareerPage)); }
        catch (Exception ex) { await DisplayAlert("Navigation Error", ex.ToString(), "OK"); }
    }

    private async void OnMyCareerTapped(object? sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(MyCareerPage));
    }

    private void ApplyFilters()
    {
        var filtered = _allCareers
            .Where(career =>
                (_selectedCategory == "All" || career.Category.Equals(_selectedCategory, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(_searchText) ||
                 career.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                 career.Description.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                 career.Category.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                 career.SalaryRangeIndia.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                 career.FutureDemand.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                 career.EducationPath.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                 career.KeySkills.Any(skill => skill.Contains(_searchText, StringComparison.OrdinalIgnoreCase)) ||
                 career.EntranceExams.Any(exam => exam.Contains(_searchText, StringComparison.OrdinalIgnoreCase)) ||
                 career.SearchAliases.Any(alias => alias.Contains(_searchText, StringComparison.OrdinalIgnoreCase)) ||
                 career.Workplaces.Any(workplace => workplace.Contains(_searchText, StringComparison.OrdinalIgnoreCase))))
            .ToList();

        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            filtered = filtered
                .OrderBy(career => GetSearchPriority(career, _searchText))
                .ThenBy(career => GetMatchingAliasLength(career, _searchText))
                .ThenBy(career => career.Name)
                .ToList();
        }
        else
        {
            filtered = filtered.OrderBy(career => career.Name).ToList();
        }

        _filteredCareers = filtered;
        _visibleCount = Math.Min(PageSize, _filteredCareers.Count);
        RefreshVisibleCareers();

        var popular = filtered.Where(career => career.IsPopular).Take(6).ToList();
        if (popular.Count == 0) popular = filtered.Take(6).ToList();
        ResetCollection(PopularCareers, popular);
    }

    private void RefreshVisibleCareers()
    {
        ResetCollection(FilteredCareers, _filteredCareers.Take(_visibleCount).ToList());
        UpdateLoadMoreFooter();
    }

    private void UpdateLoadMoreFooter()
    {
        _loadMoreButton ??= new Button
        {
            BackgroundColor = Color.FromArgb("#17203B"),
            TextColor = Colors.White,
            CornerRadius = 14,
            Padding = new Thickness(22, 10),
            FontFamily = "OpenSansSemibold",
            FontSize = 13,
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 8, 0, 24)
        };

        _loadMoreButton.Clicked -= OnLoadMoreClicked;
        _loadMoreButton.Clicked += OnLoadMoreClicked;
        _loadMoreButton.IsVisible = _visibleCount < _filteredCareers.Count;
        _loadMoreButton.Text = $"Load more careers\n{Math.Max(0, _filteredCareers.Count - _visibleCount)} remaining";

        CareerCollectionView.Footer = new VerticalStackLayout
        {
            Children = { _loadMoreButton },
            HorizontalOptions = LayoutOptions.Fill
        };
    }

    private void OnLoadMoreClicked(object? sender, EventArgs e)
    {
        _visibleCount = Math.Min(_visibleCount + PageSize, _filteredCareers.Count);
        RefreshVisibleCareers();
    }

    private static int GetSearchPriority(Career career, string searchText)
    {
        if (career.SearchAliases.Any(alias => alias.Equals(searchText, StringComparison.OrdinalIgnoreCase))) return 0;
        if (career.SearchAliases.Any(alias => alias.StartsWith(searchText, StringComparison.OrdinalIgnoreCase))) return 1;
        if (career.SearchAliases.Any(alias => alias.Contains(searchText, StringComparison.OrdinalIgnoreCase))) return 2;
        if (career.Name.StartsWith(searchText, StringComparison.OrdinalIgnoreCase)) return 3;
        if (career.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase)) return 4;
        return 5;
    }

    private static int GetMatchingAliasLength(Career career, string searchText)
    {
        var matchingAlias = career.SearchAliases
            .Where(alias => alias.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            .OrderBy(alias => alias.Length)
            .FirstOrDefault();
        return matchingAlias?.Length ?? int.MaxValue;
    }

    private void ResetCollection(ObservableCollection<Career> collection, IReadOnlyCollection<Career> items)
    {
        collection.Clear();
        foreach (var item in items) collection.Add(item);
    }

    private void ApplyCategoryStyles()
    {
        SetCategoryButtonState(AllCategoryButton, _selectedCategory == "All");
        SetCategoryButtonState(ScienceCategoryButton, _selectedCategory == "Science");
        SetCategoryButtonState(CommerceCategoryButton, _selectedCategory == "Commerce");
        SetCategoryButtonState(ArtsCategoryButton, _selectedCategory == "Arts");
    }

    private static void SetCategoryButtonState(Button button, bool isSelected)
    {
        button.BackgroundColor = isSelected ? Color.FromArgb("#17203B") : Colors.White;
        button.TextColor = isSelected ? Colors.White : Color.FromArgb("#6F7691");
    }
}
