using Microsoft.Maui.Controls.Shapes;
using StudyMap.Models;
using StudyMap.Services;

namespace StudyMap;

public partial class CareerResultsPage : ContentPage
{
    private readonly CareerRecommendationService _recommendationService = new();
    private bool _showingAll;

    public CareerResultsPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _showingAll = false;
        await LoadResultsAsync(topCount: 5);
    }

    // ── Data loading ─────────────────────────────────────────────────
    private async Task LoadResultsAsync(int topCount)
    {
        try
        {
            var answers = CareerDiscoverySessionService.Instance.Answers;

            if (answers.Count == 0)
            {
                NoMatchBanner.IsVisible = true;
                ViewAllMatchesButton.IsVisible = false;
                ResultsLayout.Children.Clear();
                return;
            }

            var recommendations = await _recommendationService.GetRecommendationsAsync(answers, topCount);
            var hasStrongMatch = recommendations.Any(r => r.Score > 0);

            NoMatchBanner.IsVisible = !hasStrongMatch;
            ViewAllMatchesButton.IsVisible = !_showingAll;

            BuildResultCards(recommendations);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CareerResultsPage] {ex.Message}");
            NoMatchBanner.IsVisible = true;
            ResultsLayout.Children.Clear();
        }
    }

    // ── Card building ────────────────────────────────────────────────
    private void BuildResultCards(List<CareerRecommendation> recommendations)
    {
        ResultsLayout.Children.Clear();

        foreach (var recommendation in recommendations)
        {
            ResultsLayout.Children.Add(CreateResultCard(recommendation));
        }
    }

    private Border CreateResultCard(CareerRecommendation recommendation)
    {
        var career = recommendation.Career;

        var iconBubble = new Border
        {
            WidthRequest = 52,
            HeightRequest = 52,
            StrokeThickness = 0,
            BackgroundColor = Color.FromArgb(career.AccentColor).WithAlpha(0.18f),
            StrokeShape = new RoundRectangle { CornerRadius = 16 },
            Content = new Label
            {
                Text = career.Icon,
                FontSize = 26,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            }
        };

        var nameLabel = new Label
        {
            Text = career.Name,
            FontFamily = "OpenSansSemibold",
            FontSize = 17,
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

        var matchBadge = new Border
        {
            StrokeThickness = 0,
            Padding = new Thickness(10, 5),
            BackgroundColor = Color.FromArgb("#EDE7FF"),
            VerticalOptions = LayoutOptions.Start,
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            Content = new Label
            {
                Text = recommendation.Score > 0 ? $"{recommendation.MatchPercentage}% Match" : "Popular",
                FontSize = 12,
                FontFamily = "OpenSansSemibold",
                TextColor = Color.FromArgb("#7C57F5")
            }
        };

        var headerGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 12
        };
        headerGrid.Add(iconBubble, 0);
        headerGrid.Add(titleStack, 1);
        headerGrid.Add(matchBadge, 2);

        var descriptionLabel = new Label
        {
            Text = career.Description,
            FontSize = 13,
            TextColor = Color.FromArgb("#6F7691"),
            LineBreakMode = LineBreakMode.WordWrap
        };

        var matchLabel = new Label
        {
            Text = "Interest Match",
            FontSize = 11,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#7C57F5")
        };

        var whyLabel = new Label
        {
            Text = recommendation.WhyItMatches,
            FontSize = 13,
            TextColor = Color.FromArgb("#6F7691"),
            LineBreakMode = LineBreakMode.WordWrap
        };

        var whyCard = new Border
        {
            StrokeThickness = 0,
            Padding = new Thickness(14, 12),
            BackgroundColor = Color.FromArgb("#F7F5FF"),
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            Content = new VerticalStackLayout
            {
                Spacing = 6,
                Children = { matchLabel, whyLabel }
            }
        };

        var exploreButton = new Button
        {
            Text = "Explore Career  →",
            BackgroundColor = Color.FromArgb("#7C57F5"),
            TextColor = Colors.White,
            FontFamily = "OpenSansSemibold",
            FontSize = 14,
            CornerRadius = 14,
            HeightRequest = 46
        };
        exploreButton.Clicked += async (_, _) => await NavigateToCareerDetail(career.Id);

        var content = new VerticalStackLayout
        {
            Spacing = 12,
            Children = { headerGrid, descriptionLabel, whyCard, exploreButton }
        };

        return new Border
        {
            StrokeThickness = 0,
            Padding = new Thickness(18),
            BackgroundColor = Colors.White,
            StrokeShape = new RoundRectangle { CornerRadius = 20 },
            Shadow = new Shadow { Brush = Color.FromArgb("#08000000"), Offset = new Point(0, 4), Radius = 12, Opacity = 1 },
            Content = content
        };
    }

    // ── Navigation ────────────────────────────────────────────────────
    private async Task NavigateToCareerDetail(string careerId)
    {
        await Shell.Current.GoToAsync(
            $"{nameof(CareerDetailPage)}?careerId={Uri.EscapeDataString(careerId)}");
    }

    private async void OnViewAllMatchesClicked(object? sender, EventArgs e)
    {
        _showingAll = true;
        await LoadResultsAsync(topCount: 999);
    }

    private async void OnExploreCareersClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//CareerExplorerPage");
    }

    private async void OnRetakeClicked(object? sender, EventArgs e)
    {
        CareerDiscoverySessionService.Instance.Reset();
        await Shell.Current.GoToAsync($"//{nameof(CareerDiscoveryPage)}");
    }

    private async void OnBackTapped(object? sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
