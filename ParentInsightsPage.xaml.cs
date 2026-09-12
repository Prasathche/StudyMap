using Microsoft.Maui.Controls.Shapes;
using StudyMap.Models;
using StudyMap.Services;

namespace StudyMap;

public partial class ParentInsightsPage : ContentPage
{
    private readonly ParentInsightsService _service = new();
    private readonly List<Career> _selectedForCompare = [];
    private List<Career> _allCareers = [];
    private string _selectedChip = "All";
    private string _searchText = string.Empty;
    private bool _compareMode;
    private bool _isLoaded;

    public ParentInsightsPage()
    {
        InitializeComponent();
        BuildCategoryChips();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_isLoaded)
        {
            return;
        }

        _isLoaded = true;
        _allCareers = await _service.GetAllCareersAsync();
        await ApplyFiltersAsync();
    }

    // ── Category chips ───────────────────────────────────────────────
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

    private async Task OnCategoryChipClicked(string chip)
    {
        _selectedChip = chip;
        foreach (var child in CategoryChipsLayout.Children)
        {
            if (child is Button btn)
            {
                SetChipStyle(btn, btn.Text == chip);
            }
        }

        await ApplyFiltersAsync();
    }

    private static void SetChipStyle(Button button, bool selected)
    {
        button.BackgroundColor = selected ? Color.FromArgb("#2E7D6B") : Colors.White;
        button.TextColor = selected ? Colors.White : Color.FromArgb("#7D849A");
    }

    // ── Search ────────────────────────────────────────────────────────
    private async void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        _searchText = e.NewTextValue ?? string.Empty;
        await ApplyFiltersAsync();
    }

    // ── Filtering ────────────────────────────────────────────────────
    private async Task ApplyFiltersAsync()
    {
        var byCategory = await _service.GetCareersByCategoryChipAsync(_selectedChip);

        var filtered = string.IsNullOrWhiteSpace(_searchText)
            ? byCategory
            : byCategory.Where(c =>
                c.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                c.Category.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
                .ToList();

        var showPopularOnly = _selectedChip == "All" && string.IsNullOrWhiteSpace(_searchText);
        var toShow = showPopularOnly
            ? filtered.Where(c => c.IsPopular).ToList()
            : filtered;

        ResultsTitleLabel.Text = showPopularOnly ? "Popular Careers" : $"{toShow.Count} Career{(toShow.Count == 1 ? "" : "s")}";

        BuildCareerCards(toShow.OrderBy(c => c.Name).ToList());
    }

    // ── Career cards ─────────────────────────────────────────────────
    private void BuildCareerCards(List<Career> careers)
    {
        CareersLayout.Children.Clear();

        foreach (var career in careers)
        {
            CareersLayout.Children.Add(CreateCareerCard(career));
        }
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

        var nameLabel = new Label
        {
            Text = career.Name,
            FontFamily = "OpenSansSemibold",
            FontSize = 16,
            TextColor = Color.FromArgb("#17203B")
        };

        var categoryLabel = new Label
        {
            Text = career.Category,
            FontSize = 12,
            TextColor = Color.FromArgb("#7D849A")
        };

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
            var chevron = new Label { Text = "›", FontSize = 22, TextColor = Color.FromArgb("#C7CCDA"), VerticalOptions = LayoutOptions.Center };
            grid.Add(chevron, 2);
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

        card.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() => OnCareerCardTapped(career))
        });

        return card;
    }

    private async void OnCareerCardTapped(Career career)
    {
        if (_compareMode)
        {
            ToggleCompareSelection(career);
            return;
        }

        await Shell.Current.GoToAsync(
            $"{nameof(CareerInsightsPage)}?careerId={Uri.EscapeDataString(career.Id)}");
    }

    // ── Compare mode ─────────────────────────────────────────────────
    private async void OnCompareToggleClicked(object? sender, EventArgs e)
    {
        _compareMode = !_compareMode;
        CompareToggleButton.Text = _compareMode ? "Cancel" : "Compare";
        CompareBar.IsVisible = _compareMode;

        if (!_compareMode)
        {
            _selectedForCompare.Clear();
        }

        UpdateCompareBar();
        await ApplyFiltersAsync();
    }

    private async void ToggleCompareSelection(Career career)
    {
        var existing = _selectedForCompare.FirstOrDefault(c => c.Id == career.Id);
        if (existing is not null)
        {
            _selectedForCompare.Remove(existing);
        }
        else if (_selectedForCompare.Count < 3)
        {
            _selectedForCompare.Add(career);
        }

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
            return;
        }

        var ids = string.Join(",", _selectedForCompare.Select(c => Uri.EscapeDataString(c.Id)));
        await Shell.Current.GoToAsync($"{nameof(CareerComparisonPage)}?careerIds={ids}");
    }
}
