using Microsoft.Maui.Controls.Shapes;
using StudyMap.Models;
using StudyMap.Services;

namespace StudyMap;

[QueryProperty(nameof(CareerIds), "careerIds")]
public partial class CareerComparisonPage : ContentPage
{
    private readonly ParentInsightsService _service = new();
    private string _careerIds = string.Empty;

    private static readonly string[] FactorLabels =
    [
        "Earning Potential",
        "Job Stability",
        "Future Demand",
        "Difficulty",
        "Competition",
        "Typical UG Duration",
        "Entrance Exams"
    ];

    public CareerComparisonPage()
    {
        InitializeComponent();
    }

    public string CareerIds
    {
        get => _careerIds;
        set
        {
            _careerIds = Uri.UnescapeDataString(value ?? string.Empty);
            MainThread.BeginInvokeOnMainThread(async () => await LoadComparisonAsync(_careerIds));
        }
    }

    private async Task LoadComparisonAsync(string careerIdsCsv)
    {
        var ids = careerIdsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (ids.Length == 0)
            return;

        var careers = new List<Career>();
        foreach (var id in ids)
        {
            var career = await _service.GetCareerByIdAsync(id);
            if (career is not null)
                careers.Add(career);
        }

        if (careers.Count == 0)
        {
            await DisplayAlert("Compare Careers", "We couldn't load the selected careers.", "OK");
            return;
        }

        BuildComparison(careers);
    }

    private void BuildComparison(List<Career> careers)
    {
        ComparisonContent.Children.Clear();
        SelectedCareersLayout.Children.Clear();

        foreach (var career in careers)
        {
            var chip = new Border
            {
                BackgroundColor = Color.FromArgb("#E4F3EF"),
                StrokeThickness = 0,
                Padding = new Thickness(10, 7),
                Margin = new Thickness(0, 0, 8, 8),
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                Content = new Label
                {
                    Text = $"{career.Icon}  {career.Name}",
                    FontFamily = "OpenSansSemibold",
                    FontSize = 12,
                    TextColor = Color.FromArgb("#2E7D6B"),
                    LineBreakMode = LineBreakMode.WordWrap
                }
            };
            SelectedCareersLayout.Children.Add(chip);
        }

        foreach (var factor in FactorLabels)
        {
            var card = new Border
            {
                BackgroundColor = Colors.White,
                StrokeThickness = 0,
                Padding = new Thickness(16),
                StrokeShape = new RoundRectangle { CornerRadius = 18 },
                Shadow = new Shadow
                {
                    Brush = Color.FromArgb("#08000000"),
                    Offset = new Point(0, 3),
                    Radius = 10,
                    Opacity = 1
                }
            };

            var content = new VerticalStackLayout { Spacing = 6 };
            content.Children.Add(new Label
            {
                Text = factor,
                FontFamily = "OpenSansSemibold",
                FontSize = 15,
                TextColor = Color.FromArgb("#17203B"),
                Margin = new Thickness(0, 0, 0, 4)
            });

            foreach (var career in careers)
            {
                var insight = _service.BuildInsight(career);
                var value = GetFactorValue(factor, insight);

                var row = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition(GridLength.Star),
                        new ColumnDefinition(GridLength.Star)
                    },
                    ColumnSpacing = 12,
                    Padding = new Thickness(0, 7)
                };

                row.Add(new Label
                {
                    Text = $"{career.Icon} {career.Name}",
                    FontFamily = "OpenSansSemibold",
                    FontSize = 12,
                    TextColor = Color.FromArgb("#5C6478"),
                    VerticalOptions = LayoutOptions.Start,
                    LineBreakMode = LineBreakMode.WordWrap
                }, 0);

                row.Add(new Label
                {
                    Text = value,
                    FontSize = 13,
                    TextColor = Color.FromArgb("#17203B"),
                    HorizontalTextAlignment = TextAlignment.End,
                    VerticalOptions = LayoutOptions.Start,
                    LineBreakMode = LineBreakMode.WordWrap
                }, 1);

                content.Children.Add(row);
            }

            card.Content = content;
            ComparisonContent.Children.Add(card);
        }
    }

    private static string GetFactorValue(string factor, CareerParentInsight insight) => factor switch
    {
        "Earning Potential" => insight.SalaryRangeText,
        "Job Stability" => insight.StabilityLevel,
        "Future Demand" => insight.FutureDemandLevel,
        "Difficulty" => insight.DifficultyLevel,
        "Competition" => insight.CompetitionLevel,
        "Typical UG Duration" => insight.TypicalUGDuration,
        "Entrance Exams" => insight.EntranceExams.Count == 0
            ? "No specific exam listed"
            : string.Join(", ", insight.EntranceExams),
        _ => "Information unavailable"
    };

    private async Task GoBackToDecideAsync()
    {
        if (Shell.Current.Navigation.NavigationStack.Count > 1)
        {
            await Shell.Current.Navigation.PopAsync();
            return;
        }

        await Shell.Current.GoToAsync("//ParentInsightsPage");
    }

    private async void OnBackTapped(object? sender, EventArgs e)
    {
        await GoBackToDecideAsync();
    }

    private async void OnBackToDecideTapped(object? sender, EventArgs e)
    {
        await GoBackToDecideAsync();
    }

    private async void OnHomeTapped(object? sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(MyCareerPage));
    }
}
