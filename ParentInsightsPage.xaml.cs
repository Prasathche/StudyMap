using Microsoft.Maui.Controls.Shapes;
using StudyMap.Models;
using StudyMap.Services;

namespace StudyMap;

public partial class ParentInsightsPage : ContentPage
{
    private const int PageSize = 25;

    private readonly ParentInsightsService _service = new();
    private readonly List<Career> _selectedForCompare = [];
    private List<Career> _allCareers = [];
    private List<Career> _filteredCareers = [];
    private string _selectedChip = "All";
    private string _searchText = string.Empty;
    private bool _compareMode;
    private bool _isLoaded;
    private int _visibleCount;
    private Button? _loadMoreButton;

    public ParentInsightsPage()
    {
        InitializeComponent();
        BuildCategoryChips();
        BuildLoadMoreButton();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_isLoaded) return;

        _isLoaded = true;
        _allCareers = await _service.GetAllCareersAsync();
        await ApplyFiltersAsync();
    }

    private void BuildCategoryChips()
    {
        CategoryChipsLayout.Children.Clear();
        var chips = new List<string> { "All" };
        chips.AddRange(ParentInsightsService.CategoryChips.Select(c => c.Chip));

        foreach (var chip in chips)
        {
            var button = new Button
            {
                Text = chip,
                CornerRadius = 16,
                Padding = new Thickness(16, 8),
                FontFamily = "OpenSansSemibold",
                FontSize = 13,
                MinimumHeightRequest = 0,
                MinimumWidthRequest = 0
            };
            SetChipStyle(button, chip == _selectedChip);
            button.Clicked += async (_, _) => await OnCategoryChipClicked(chip);
            CategoryChipsLayout.Children.Add(button);
        }
    }

    private void BuildLoadMoreButton()
    {
        _loadMoreButton = new Button
        {
            Text = "Load more careers",
            BackgroundColor = Color.FromArgb("#2E7D6B"),
            TextColor = Colors.White,
            CornerRadius = 14,
            Padding = new Thickness(22, 10),
            FontFamily = "OpenSansSemibold",
            FontSize = 13,
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 6, 0, 20)
        };
        _loadMoreButton.Clicked += OnLoadMoreClicked;
    }

    private async void OnHomeTapped(object? sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(MyCareerPage));
    }

    private async Task OnCategoryChipClicked(string chip)
    {
        _selectedChip = chip;
        foreach (var child in CategoryChipsLayout.Children)
            if (child is Button btn) SetChipStyle(btn, btn.Text == chip);

        await ApplyFiltersAsync();
    }

    private static void SetChipStyle(Button button, bool selected)
    {
        button.BackgroundColor = selected ? Color.FromArgb("#2E7D6B") : Colors.White;
        button.TextColor = selected ? Colors.White : Color.FromArgb("#7D849A");
    }

    private async void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        _searchText = e.NewTextValue ?? string.Empty;
        await ApplyFiltersAsync();
    }

    private async Task ApplyFiltersAsync()
    {
        var byCategory = await _service.GetCareersByCategoryChipAsync(_selectedChip);

        var query = _searchText.Trim();
        var filtered = string.IsNullOrWhiteSpace(query)
            ? byCategory
            : byCategory.Where(c =>
                c.SearchAliases.Any(a => a.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                c.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                c.Category.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                c.CareerCategories.Any(t => t.Contains(query, StringComparison.OrdinalIgnoreCase)))
                .ToList();

        var showPopularOnly = _selectedChip == "All" && string.IsNullOrWhiteSpace(query);
        _filteredCareers = (showPopularOnly
            ? filtered.Where(c => c.IsPopular)
            : filtered)
            .OrderBy(c => GetSearchRank(c, query))
            .ThenBy(c => c.Name)
            .ToList();

        _visibleCount = Math.Min(PageSize, _filteredCareers.Count);
        ResultsTitleLabel.Text = showPopularOnly
            ? "Popular Careers"
            : $"{_filteredCareers.Count} Career{(_filteredCareers.Count == 1 ? "" : "s")}";

        BuildCareerCards(_filteredCareers.Take(_visibleCount).ToList());
        UpdateLoadMoreButton();
    }

    private static int GetSearchRank(Career career, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return 100;

        var aliases = career.SearchAliases ?? [];
        var normalizedQuery = query.Trim();

        // Search aliases intentionally have priority over the display name and
        // broader category matches. This makes common terms such as "doctor",
        // "CA", "CS" or other aliases surface the intended career first.
        if (aliases.Any(a => string.Equals(a.Trim(), normalizedQuery, StringComparison.OrdinalIgnoreCase)))
            return 0;

        if (aliases.Any(a => a.StartsWith(normalizedQuery, StringComparison.OrdinalIgnoreCase)))
            return 1;

        if (aliases.Any(a => a.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase)))
            return 2;

        if (string.Equals(career.Name.Trim(), normalizedQuery, StringComparison.OrdinalIgnoreCase))
            return 3;

        if (career.Name.StartsWith(normalizedQuery, StringComparison.OrdinalIgnoreCase))
            return 4;

        if (career.Name.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase))
            return 5;

        if (career.CareerCategories.Any(t => t.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase)))
            return 6;

        if (career.Category.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase))
            return 7;

        return 8;
    }

    private void OnLoadMoreClicked(object? sender, EventArgs e)
    {
        _visibleCount = Math.Min(_visibleCount + PageSize, _filteredCareers.Count);
        BuildCareerCards(_filteredCareers.Take(_visibleCount).ToList());
        UpdateLoadMoreButton();
    }

    private void UpdateLoadMoreButton()
    {
        if (_loadMoreButton is null) return;

        CareersLayout.Children.Remove(_loadMoreButton);
        if (_visibleCount < _filteredCareers.Count)
        {
            _loadMoreButton.Text = $"Load more careers ({_filteredCareers.Count - _visibleCount} remaining)";
            CareersLayout.Children.Add(_loadMoreButton);
        }
    }

    private void BuildCareerCards(List<Career> careers)
    {
        CareersLayout.Children.Clear();
        foreach (var career in careers)
            CareersLayout.Children.Add(CreateCareerCard(career));
    }

    private Border CreateCareerCard(Career career)
    {
        var isSelected = _selectedForCompare.Any(c => c.Id == career.Id);

        var iconBubble = new Border
        {
            WidthRequest = 48,
            HeightRequest = 48,
            StrokeThickness = 0,
            BackgroundColor = Color.FromArgb(career.AccentColor).WithAlpha(0.18f),
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            Content = new Label { Text = career.Icon, FontSize = 24, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
        };

        var nameLabel = new Label { Text = career.Name, FontFamily = "OpenSansSemibold", FontSize = 16, TextColor = Color.FromArgb("#17203B") };
        var categoryLabel = new Label { Text = career.Category, FontSize = 12, TextColor = Color.FromArgb("#7D849A") };

        var titleStack = new VerticalStackLayout
        {
            Spacing = 2,
            VerticalOptions = LayoutOptions.Center,
            Children = { nameLabel, categoryLabel }
        };

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 12
        };
        grid.Add(iconBubble, 0);
        grid.Add(titleStack, 1);

        if (_compareMode)
        {
            var checkCircle = new Border
            {
                WidthRequest = 26,
                HeightRequest = 26,
                StrokeThickness = 1.5,
                Stroke = isSelected ? Colors.Transparent : Color.FromArgb("#D6DAE6"),
                BackgroundColor = isSelected ? Color.FromArgb("#2E7D6B") : Colors.White,
                StrokeShape = new RoundRectangle { CornerRadius = 13 },
                VerticalOptions = LayoutOptions.Center,
                Content = isSelected
                    ? new Label { Text = "✓", TextColor = Colors.White, FontSize = 13, FontAttributes = FontAttributes.Bold, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
                    : null
            };
            grid.Add(checkCircle, 2);
        }
        else
        {
            grid.Add(new Label { Text = "›", FontSize = 22, TextColor = Color.FromArgb("#C7CCDA"), VerticalOptions = LayoutOptions.Center }, 2);
        }

        var card = new Border
        {
            Padding = new Thickness(16),
            StrokeThickness = _compareMode && isSelected ? 1.5 : 0,
            Stroke = Color.FromArgb("#2E7D6B"),
            BackgroundColor = Colors.White,
            StrokeShape = new RoundRectangle { CornerRadius = 18 },
            Shadow = new Shadow { Brush = Color.FromArgb("#08000000"), Offset = new Point(0, 4), Radius = 12, Opacity = 1 },
            Content = grid
        };

        card.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => OnCareerCardTapped(career)) });
        return card;
    }

    private async void OnCareerCardTapped(Career career)
    {
        if (_compareMode)
        {
            ToggleCompareSelection(career);
            return;
        }

        await Shell.Current.GoToAsync($"{nameof(CareerInsightsPage)}?careerId={Uri.EscapeDataString(career.Id)}");
    }

    private async void OnCompareToggleClicked(object? sender, EventArgs e)
    {
        _compareMode = !_compareMode;
        CompareToggleButton.Text = _compareMode ? "Cancel" : "Compare";
        CompareBar.IsVisible = _compareMode;

        if (!_compareMode) _selectedForCompare.Clear();

        UpdateCompareBar();
        await ApplyFiltersAsync();
    }

    private async void ToggleCompareSelection(Career career)
    {
        var existing = _selectedForCompare.FirstOrDefault(c => c.Id == career.Id);
        if (existing is not null)
            _selectedForCompare.Remove(existing);
        else if (_selectedForCompare.Count < 3)
            _selectedForCompare.Add(career);

        UpdateCompareBar();
        await ApplyFiltersAsync();
    }

    private void UpdateCompareBar()
    {
        var count = _selectedForCompare.Count;
        CompareCountLabel.Text = count == 0
            ? "Select 2-3 careers to compare"
            : $"{count} career{(count == 1 ? "" : "s")} selected";

        CompareGoButton.IsEnabled = count >= 2;
        CompareGoButton.Opacity = count >= 2 ? 1 : 0.5;
    }

    private async void OnCompareGoClicked(object? sender, EventArgs e)
    {
        if (_selectedForCompare.Count < 2)
        {
            await DisplayAlert("Compare Careers", "Please select at least 2 careers to compare.", "OK");
            return;
        }

        var ids = string.Join(",", _selectedForCompare.Select(c => Uri.EscapeDataString(c.Id)));
        _compareMode = false;
        _selectedForCompare.Clear();
        CompareToggleButton.Text = "Compare";
        CompareBar.IsVisible = false;
        await ApplyFiltersAsync();
        await Shell.Current.GoToAsync($"{nameof(CareerComparisonPage)}?careerIds={ids}");
    }
}