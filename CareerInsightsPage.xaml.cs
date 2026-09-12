using Microsoft.Maui.Controls.Shapes;
using StudyMap.Models;
using StudyMap.Services;

namespace StudyMap;

[QueryProperty(nameof(CareerId), "careerId")]
public partial class CareerInsightsPage : ContentPage
{
    private readonly ParentInsightsService _service = new();
    private string _careerId = string.Empty;

    public CareerInsightsPage()
    {
        InitializeComponent();
    }

    // ── QueryProperty ────────────────────────────────────────────────
    public string CareerId
    {
        get => _careerId;
        set
        {
            _careerId = Uri.UnescapeDataString(value ?? string.Empty);
            MainThread.BeginInvokeOnMainThread(async () => await LoadInsightAsync(_careerId));
        }
    }

    // ── Data loading ─────────────────────────────────────────────────
    private async Task LoadInsightAsync(string careerId)
    {
        if (string.IsNullOrWhiteSpace(careerId))
        {
            return;
        }

        LoadingOverlay.IsVisible = true;

        var career = await _service.GetCareerByIdAsync(careerId);
        if (career is null)
        {
            LoadingOverlay.IsVisible = false;
            await DisplayAlert("Parent Insights", "We couldn't find information for this career.", "OK");
            return;
        }

        var insight = _service.BuildInsight(career);
        Populate(career, insight);

        LoadingOverlay.IsVisible = false;
    }

    // ── UI population ─────────────────────────────────────────────────
    private void Populate(Career career, CareerParentInsight insight)
    {
        CareerIconLabel.Text = career.Icon;
        CareerNameLabel.Text = career.Name;
        CareerCategoryLabel.Text = $"{career.Category} Stream";
        SnapshotLabel.Text = career.Description;

        BuildIndicatorCards(insight);

        DurationLabel.Text = insight.TypicalUGDuration;
        BuildChips(UgCoursesLayout, insight.UGCourses, "#E4F3EF", "#2E7D6B");
        BuildChips(PgCoursesLayout, insight.PGCourses, "#EEF0F6", "#5C6478");

        ExamsSection.IsVisible = insight.EntranceExams.Count > 0;
        BuildChips(ExamsLayout, insight.EntranceExams, "#FFF2E7", "#B85C26");

        BuildCheckList(ConsiderationsLayout, insight.ParentConsiderations, "❓", "#2E7D6B", "#E4F3EF");
        BuildChips(SuitableForLayout, insight.SuitableFor, "#E4F3EF", "#2E7D6B");
        BuildCheckList(ThingsToConsiderLayout, insight.ThingsToConsider, "•", "#B85C26", "#FFF2E7");
    }

    private void BuildIndicatorCards(CareerParentInsight insight)
    {
        IndicatorsLayout.Children.Clear();

        IndicatorsLayout.Children.Add(CreateIndicatorCard(
            "💰", "Earning Potential", string.Empty, insight.SalaryRangeText, insight.SalaryDescription));

        IndicatorsLayout.Children.Add(CreateIndicatorCard(
            "🧭", "Job Stability", insight.StabilityIndicator, insight.StabilityLevel, insight.StabilityDescription));

        IndicatorsLayout.Children.Add(CreateIndicatorCard(
            "📈", "Future Demand", insight.FutureDemandIndicator, insight.FutureDemandLevel, insight.FutureDemandDescription));

        IndicatorsLayout.Children.Add(CreateIndicatorCard(
            "🎓", "Education & Skill Difficulty", insight.DifficultyIndicator, insight.DifficultyLevel, insight.DifficultyDescription));

        IndicatorsLayout.Children.Add(CreateIndicatorCard(
            "🏁", "Competition", insight.CompetitionIndicator, insight.CompetitionLevel, insight.CompetitionDescription));
    }

    private static Border CreateIndicatorCard(string icon, string title, string indicator, string level, string description)
    {
        var headerLabel = new Label
        {
            Text = $"{icon} {title}",
            FontFamily = "OpenSansSemibold",
            FontSize = 15,
            TextColor = Color.FromArgb("#17203B")
        };

        var levelLabel = new Label
        {
            Text = string.IsNullOrEmpty(indicator) ? level : $"{indicator} {level}",
            FontFamily = "OpenSansSemibold",
            FontSize = 17,
            TextColor = Color.FromArgb("#17203B")
        };

        var descriptionLabel = new Label
        {
            Text = description,
            FontSize = 13,
            TextColor = Color.FromArgb("#6F7691"),
            LineBreakMode = LineBreakMode.WordWrap
        };

        var content = new VerticalStackLayout
        {
            Spacing = 8,
            Children = { headerLabel, levelLabel, descriptionLabel }
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

    private static void BuildChips(FlexLayout layout, IEnumerable<string> items, string bg, string fg)
    {
        layout.Children.Clear();
        foreach (var item in items)
        {
            var chip = new Border
            {
                StrokeThickness = 0,
                Padding = new Thickness(12, 6, 16, 6),
                Margin = new Thickness(0, 0, 8, 8),
                BackgroundColor = Color.FromArgb(bg),
                StrokeShape = new RoundRectangle { CornerRadius = 10 },
                Content = new Label { Text = item, FontSize = 13, TextColor = Color.FromArgb(fg), LineBreakMode = LineBreakMode.NoWrap }
            };
            layout.Children.Add(chip);
        }

        if (!layout.Children.Any())
        {
            layout.Children.Add(new Label { Text = "Information unavailable", FontSize = 13, TextColor = Color.FromArgb("#7D849A") });
        }
    }

    private static void BuildCheckList(VerticalStackLayout layout, IEnumerable<string> items, string bullet, string bulletColor, string bubbleBg)
    {
        layout.Children.Clear();
        foreach (var item in items)
        {
            var bulletBubble = new Border
            {
                StrokeThickness = 0,
                BackgroundColor = Color.FromArgb(bubbleBg),
                Padding = new Thickness(7, 3),
                VerticalOptions = LayoutOptions.Start,
                StrokeShape = new RoundRectangle { CornerRadius = 8 },
                Content = new Label { Text = bullet, FontSize = 13, TextColor = Color.FromArgb(bulletColor) }
            };

            var textLabel = new Label
            {
                Text = item,
                FontSize = 14,
                TextColor = Color.FromArgb("#6F7691"),
                LineBreakMode = LineBreakMode.WordWrap,
                HorizontalOptions = LayoutOptions.Fill
            };

            var row = new HorizontalStackLayout
            {
                Spacing = 10,
                Children = { bulletBubble, textLabel }
            };

            layout.Children.Add(row);
        }
    }

    // ── Navigation ────────────────────────────────────────────────────
    private async void OnViewRoadmapClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(CareerRoadmapPage));
    }

    private async void OnBackTapped(object? sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
