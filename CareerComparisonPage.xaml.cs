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
        "Education Cost"
    ];

    public CareerComparisonPage()
    {
        InitializeComponent();
    }

    // ── QueryProperty ────────────────────────────────────────────────
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
        {
            return;
        }

        var careers = new List<Career>();
        foreach (var id in ids)
        {
            var career = await _service.GetCareerByIdAsync(id);
            if (career is not null)
            {
                careers.Add(career);
            }
        }

        if (careers.Count == 0)
        {
            await DisplayAlert("Compare Careers", "We couldn't load the selected careers.", "OK");
            return;
        }

        BuildComparisonGrid(careers);
    }

    private void BuildComparisonGrid(List<Career> careers)
    {
        ComparisonGrid.Children.Clear();
        ComparisonGrid.RowDefinitions.Clear();
        ComparisonGrid.ColumnDefinitions.Clear();

        // Column 0 = factor labels, one column per career after that.
        ComparisonGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(130)));
        foreach (var _ in careers)
        {
            ComparisonGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(130)));
        }

        var rowCount = FactorLabels.Length + 1; // + header row
        for (var i = 0; i < rowCount; i++)
        {
            ComparisonGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        }

        // Header row: career icon + name.
        AddCell(0, 0, string.Empty, isHeader: true);
        for (var c = 0; c < careers.Count; c++)
        {
            var career = careers[c];
            var headerStack = new VerticalStackLayout
            {
                Spacing = 4,
                HorizontalOptions = LayoutOptions.Center,
                Children =
                {
                    new Label { Text = career.Icon, FontSize = 24, HorizontalOptions = LayoutOptions.Center },
                    new Label
                    {
                        Text = career.Name,
                        FontFamily = "OpenSansSemibold",
                        FontSize = 12,
                        TextColor = Color.FromArgb("#17203B"),
                        HorizontalTextAlignment = TextAlignment.Center,
                        LineBreakMode = LineBreakMode.WordWrap,
                        MaxLines = 2
                    }
                }
            };
            Grid.SetRow(headerStack, 0);
            Grid.SetColumn(headerStack, c + 1);
            ComparisonGrid.Children.Add(headerStack);
        }

        // Factor rows.
        for (var f = 0; f < FactorLabels.Length; f++)
        {
            var row = f + 1;
            AddCell(row, 0, FactorLabels[f], isHeader: true);

            for (var c = 0; c < careers.Count; c++)
            {
                var insight = _service.BuildInsight(careers[c]);
                var value = FactorLabels[f] switch
                {
                    "Earning Potential" => insight.SalaryRangeText,
                    "Job Stability" => insight.StabilityLevel,
                    "Future Demand" => insight.FutureDemandLevel,
                    "Difficulty" => insight.DifficultyLevel,
                    "Competition" => insight.CompetitionLevel,
                    "Typical UG Duration" => insight.TypicalUGDuration,
                    "Education Cost" => "Information unavailable",
                    _ => "Information unavailable"
                };

                AddCell(row, c + 1, value, isHeader: false);
            }
        }
    }

    private void AddCell(int row, int column, string text, bool isHeader)
    {
        var label = new Label
        {
            Text = text,
            FontSize = isHeader ? 12 : 13,
            FontFamily = isHeader ? "OpenSansSemibold" : "OpenSansRegular",
            TextColor = isHeader ? Color.FromArgb("#17203B") : Color.FromArgb("#6F7691"),
            LineBreakMode = LineBreakMode.WordWrap,
            HorizontalTextAlignment = column == 0 ? TextAlignment.Start : TextAlignment.Center,
            VerticalOptions = LayoutOptions.Center
        };

        var cell = new Border
        {
            Padding = new Thickness(8, 12),
            StrokeThickness = 0,
            BackgroundColor = row % 2 == 0 ? Colors.Transparent : Color.FromArgb("#F7F8FC"),
            Content = label
        };

        Grid.SetRow(cell, row);
        Grid.SetColumn(cell, column);
        ComparisonGrid.Children.Add(cell);
    }

    private async void OnBackTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
