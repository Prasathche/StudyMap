using System.Collections.ObjectModel;
using System.Text.Json;
using StudyMap.Models;
using StudyMap.Services;
using StudyMap.ViewModels;

namespace StudyMap;

[QueryProperty(nameof(CareerId), "careerId")]
public partial class CareerDetailPage : ContentPage
{
    private readonly CareerDetailsViewModel _vm;
    private string _careerId = string.Empty;

    public CareerDetailPage()
    {
        InitializeComponent();
        _vm = new CareerDetailsViewModel();
        BindingContext = _vm;
    }

    // ── QueryProperty ────────────────────────────────────────────────
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

        if (!string.IsNullOrWhiteSpace(_careerId))
            await LoadCareerAsync(_careerId);
    }

    // ── Data loading ─────────────────────────────────────────────────
    private async Task LoadCareerAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return;

        LoadingOverlay.IsVisible = true;
        await _vm.LoadCareerAsync(id);
        LoadingOverlay.IsVisible = false;

        if (_vm.Career is not null)
        {
            UpdateHeroUI(_vm.Career);
            UpdateFavoriteButton();
            BuildSkillChips(_vm.Career.KeySkills);
            BuildExamChips(_vm.Career.EntranceExams);
            BuildBulletList(SalaryInsightsLayout, _vm.Career.SalaryInsights, "#B85C26", "#FFF2E7");
            BuildStepList(RoadmapLayout, _vm.Career.RoadmapSteps, "#1A56C6", "#DCEEFF");
            BuildBulletList(CollegesLayout, _vm.Career.TopColleges, "#2F5FAF", "#EEF5FF");
            BuildBulletList(WorkplacesLayout, _vm.Career.Workplaces, "#6A1B9A", "#EDD5F7");
            RelatedCareersCV.ItemsSource = _vm.RelatedCareerObjects;
        }
    }

    // ── Hero UI ───────────────────────────────────────────────────────
    private void UpdateHeroUI(Career career)
    {
        // Capture the values first. The detail page can be revisited many times
        // and the hero labels must always be refreshed on the UI thread.
        var salary = !string.IsNullOrWhiteSpace(career.SalaryRangeIndia)
            ? career.SalaryRangeIndia
            : ExtractSalaryRange(career);

        var demand = !string.IsNullOrWhiteSpace(career.FutureDemand)
            ? career.FutureDemand
            : ExtractDemand(career);

        var stream = !string.IsNullOrWhiteSpace(career.Category)
            ? career.Category
            : "Not specified";

        MainThread.BeginInvokeOnMainThread(() =>
        {
            HeroIconLabel.Text = career.Icon;
            CareerNameLabel.Text = career.Name;
            CategoryLabel.Text = $"{stream} Stream";

            SalaryLabel.Text = salary;
            DemandLabel.Text = demand;
            StreamLabel.Text = stream;

            DescriptionLabel.Text = career.Description;
            EducationPathLabel.Text = career.EducationPath;

        });

        // Apply accent colour to hero strip gradient
        var accent = Color.FromArgb(career.AccentColor);
        var lighter = Color.FromArgb(career.AccentColor).WithLuminosity(0.7f);
        StripGradA.Color = accent;
        StripGradB.Color = lighter;

        // Set header gradient by category
        switch (career.Category.ToLowerInvariant())
        {
            case "science":
                HeroGradA.Color = Color.FromArgb("#E3F2FD");
                HeroGradB.Color = Color.FromArgb("#BBDEFB");
                break;
            case "commerce":
                HeroGradA.Color = Color.FromArgb("#E8F5E9");
                HeroGradB.Color = Color.FromArgb("#C8E6C9");
                break;
            case "arts":
                HeroGradA.Color = Color.FromArgb("#F3E5F5");
                HeroGradB.Color = Color.FromArgb("#E1BEE7");
                break;
        }
    }

    private static string ExtractSalaryRange(Career career)
    {
        var insight = career.SalaryInsights.FirstOrDefault(x =>
            x.Contains("INR", StringComparison.OrdinalIgnoreCase) &&
            x.Contains("LPA", StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(insight))
            return "Not specified";

        var start = insight.IndexOf("INR", StringComparison.OrdinalIgnoreCase);
        var end = insight.IndexOf("LPA", start, StringComparison.OrdinalIgnoreCase);

        if (start >= 0 && end > start)
        {
            var value = insight[start..(end + 3)];
            var comma = value.IndexOf(',');
            if (comma > 0)
                value = value[..comma];

            return value.Trim();
        }

        return "Not specified";
    }

    private static string ExtractDemand(Career career)
    {
        var insight = career.SalaryInsights.LastOrDefault(x =>
            x.Contains("Demand", StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(insight))
            return "Not specified";

        const string prefix = "Demand is ";
        var start = insight.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
            return insight.Trim();

        start += prefix.Length;
        var end = insight.IndexOf(" across", start, StringComparison.OrdinalIgnoreCase);
        if (end < 0)
            end = insight.Length;

        return insight[start..end].TrimEnd('.', ' ');
    }

    // ── Dynamic chip/list builders ────────────────────────────────────
    private void BuildSkillChips(IEnumerable<string> skills)
    {
        SkillsLayout.Children.Clear();
        foreach (var skill in skills)
            SkillsLayout.Children.Add(MakeChip(skill, "#EEF5FF", "#2F5FAF"));
    }

    private void BuildExamChips(IEnumerable<string> exams)
    {
        ExamsLayout.Children.Clear();
        foreach (var exam in exams)
            ExamsLayout.Children.Add(MakeChip(exam, "#FFF2E7", "#B85C26"));
    }

    private static Border MakeChip(string text, string bg, string fg)
    {
        var chip = new Border
        {
            StrokeThickness = 0,
            Padding = new Thickness(12, 5, 16, 5),
            Margin = new Thickness(0, 0, 8, 8)
        };
        chip.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 };
        chip.BackgroundColor = Color.FromArgb(bg);
        chip.Content = new Label
        {
            Text = text,
            FontSize = 13,
            TextColor = Color.FromArgb(fg)
        };
        return chip;
    }

    private static void BuildBulletList(VerticalStackLayout layout,
        IEnumerable<string> items, string bulletColor, string bubbleBg)
    {
        layout.Children.Clear();
        foreach (var item in items)
        {
            var row = new HorizontalStackLayout { Spacing = 10 };

            var bullet = new Border
            {
                StrokeThickness = 0,
                BackgroundColor = Color.FromArgb(bubbleBg),
                Padding = new Thickness(7, 3),
                VerticalOptions = LayoutOptions.Start
            };
            bullet.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 };
            bullet.Content = new Label
            {
                Text = "•",
                FontSize = 16,
                TextColor = Color.FromArgb(bulletColor)
            };

            var lbl = new Label
            {
                Text = item,
                FontSize = 14,
                TextColor = Color.FromArgb("#6F7691"),
                LineBreakMode = LineBreakMode.WordWrap,
                HorizontalOptions = LayoutOptions.Fill
            };

            row.Children.Add(bullet);
            row.Children.Add(lbl);
            layout.Children.Add(row);
        }
    }

    private static void BuildStepList(VerticalStackLayout layout,
        IEnumerable<string> steps, string bulletColor, string bubbleBg)
    {
        layout.Children.Clear();
        int i = 1;
        foreach (var step in steps)
        {
            var row = new HorizontalStackLayout { Spacing = 12 };

            var numBubble = new Border
            {
                StrokeThickness = 0,
                BackgroundColor = Color.FromArgb(bubbleBg),
                WidthRequest = 28,
                HeightRequest = 28,
                VerticalOptions = LayoutOptions.Start
            };
            numBubble.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 };
            numBubble.Content = new Label
            {
                Text = $"{i}",
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb(bulletColor),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            var lbl = new Label
            {
                Text = step,
                FontSize = 14,
                TextColor = Color.FromArgb("#6F7691"),
                LineBreakMode = LineBreakMode.WordWrap,
                HorizontalOptions = LayoutOptions.Fill
            };

            row.Children.Add(numBubble);
            row.Children.Add(lbl);
            layout.Children.Add(row);
            i++;
        }
    }

    // ── Button handlers ───────────────────────────────────────────────
    private void OnToggleFavoriteClicked(object? sender, EventArgs e)
    {
        _vm.ToggleFavoriteCommand.Execute(null);
        UpdateFavoriteButton();
        ShowStatus(_vm.IsFavorite ? "Added to Favorites!" : "Removed from Favorites");
    }

    private void UpdateFavoriteButton()
    {
        if (_vm.IsFavorite)
        {
            FavBtn.Text = "\u2764 Remove Favorite";
            FavBtn.BackgroundColor = Color.FromArgb("#FDECEA");
            FavBtn.TextColor = Color.FromArgb("#C62828");
        }
        else
        {
            FavBtn.Text = "\u2661 Add to Favorites";
            FavBtn.BackgroundColor = Colors.White;
            FavBtn.TextColor = Color.FromArgb("#17203B");
        }
    }

    private void OnSaveCareerClicked(object? sender, EventArgs e)
    {
        _vm.SaveCareerCommand.Execute(null);
        SaveBtn.Text = "\u2713 Saved!";
        SaveBtn.BackgroundColor = Color.FromArgb("#E8F5E9");
        SaveBtn.TextColor = Color.FromArgb("#2E7D32");
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Task.Delay(2500);
            SaveBtn.Text = "\U0001f4be Save Career";
            SaveBtn.BackgroundColor = Colors.White;
            SaveBtn.TextColor = Color.FromArgb("#17203B");
        });
    }

    private async void OnViewRoadmapClicked(object? sender, EventArgs e)
    {
        await RoadmapBtn.ScaleTo(0.96, 80, Easing.CubicIn);
        await RoadmapBtn.ScaleTo(1.0, 120, Easing.CubicOut);
        await Shell.Current.GoToAsync(nameof(CareerRoadmapPage));
    }

    private async void OnViewParentInsightsClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(
            $"{nameof(CareerInsightsPage)}?careerId={Uri.EscapeDataString(_careerId)}");
    }

    private async void OnBackTapped(object? sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void OnRelatedCareerTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is Career career)
        {
            await Shell.Current.GoToAsync(
                $"{nameof(CareerDetailPage)}?careerId={Uri.EscapeDataString(career.Id)}");
        }
    }

    // ── Status banner ─────────────────────────────────────────────────
    private void ShowStatus(string msg)
    {
        StatusLabel.Text = msg;
        StatusLabel.IsVisible = true;
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Task.Delay(2200);
            StatusLabel.IsVisible = false;
        });
    }
}