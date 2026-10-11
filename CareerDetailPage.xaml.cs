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
            BuildInternationalOpportunity(_vm.Career);
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
        HeroIconLabel.Text = career.Icon;
        CareerNameLabel.Text = career.Name;
        CategoryLabel.Text = $"{career.Category} Stream";

        // Salary, demand and stream are bound directly from the Career model in XAML.
        DescriptionLabel.Text = career.Description;
        EducationPathLabel.Text = career.EducationPath;

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

    private void BuildInternationalOpportunity(Career career)
    {
        var scrollView = FindScrollView(Content);
        if (scrollView?.Content is not VerticalStackLayout body)
            return;

        // Keep the section immediately after "About This Career".
        for (var i = body.Children.Count - 1; i >= 0; i--)
        {
            if (body.Children[i] is Border border && border.ClassId == "InternationalOpportunity")
                body.Children.RemoveAt(i);
        }

        var (level, summary) = GetInternationalOpportunity(career);

        var levelLabel = new Label
        {
            Text = level,
            FontFamily = "OpenSansSemibold",
            FontSize = 15,
            TextColor = Color.FromArgb("#2E7D32")
        };

        var summaryLabel = new Label
        {
            Text = summary,
            FontSize = 14,
            TextColor = Color.FromArgb("#7D849A"),
            LineBreakMode = LineBreakMode.WordWrap,
            HorizontalOptions = LayoutOptions.Fill
        };

        var content = new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                new Label
                {
                    Text = "🌍 International Opportunity",
                    FontFamily = "OpenSansSemibold",
                    FontSize = 17,
                    TextColor = Color.FromArgb("#17203B")
                },
                levelLabel,
                summaryLabel
            }
        };

        var card = new Border
        {
            ClassId = "InternationalOpportunity",
            StrokeThickness = 0,
            BackgroundColor = Colors.White,
            Padding = 20,
            Content = content
        };
        card.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 20 };
        card.Shadow = new Shadow
        {
            Brush = Color.FromArgb("#08000000"),
            Offset = new Point(0, 4),
            Radius = 12,
            Opacity = 1
        };

        // Index 2 is the existing About This Career card.
        body.Children.Insert(Math.Min(3, body.Children.Count), card);
    }

    private static (string Level, string Summary) GetInternationalOpportunity(Career career)
    {
        var text = $"{career.Name} {career.Category}".ToLowerInvariant();

        if (text.Contains("software") || text.Contains("developer") || text.Contains("computer") ||
            text.Contains("data scientist") || text.Contains("data analyst") || text.Contains("machine learning") ||
            text.Contains("cyber") || text.Contains("cloud") || text.Contains("devops") || text.Contains("technology"))
        {
            return ("🟢 Very High", $"{career.Name} is one of the careers with strong international mobility.");
        }

        if (text.Contains("doctor") || text.Contains("physician") || text.Contains("dentist") ||
            text.Contains("nurse") || text.Contains("pharmac") || text.Contains("physiotherapist") ||
            text.Contains("medical") || text.Contains("health") || text.Contains("therapist"))
        {
            return ("🟢 High", $"{career.Name} has good international opportunities, although local licensing or registration may be required.");
        }

        if (text.Contains("acca") || text.Contains("accountant") || text.Contains("chartered") ||
            text.Contains("finance") || text.Contains("investment") || text.Contains("financial") ||
            text.Contains("business analyst") || text.Contains("supply chain") || text.Contains("marketing"))
        {
            return ("🟢 High", $"{career.Name} can offer strong opportunities with multinational companies and overseas employers.");
        }

        if (text.Contains("engineer") || text.Contains("engineering") || text.Contains("architect") ||
            text.Contains("designer") || text.Contains("scientist") || text.Contains("research") ||
            text.Contains("aviation") || text.Contains("pilot"))
        {
            return ("🟢 High", $"{career.Name} can have good international opportunities, depending on the country, qualification and experience required.");
        }

        return ("🟡 Good", $"{career.Name} can provide international opportunities as you build relevant qualifications, skills and experience.");
    }

    private static ScrollView? FindScrollView(Element? element)
    {
        if (element is ScrollView scrollView)
            return scrollView;

        if (element is IElementController controller)
        {
            foreach (var child in controller.LogicalChildren)
            {
                var result = FindScrollView(child as Element);
                if (result is not null)
                    return result;
            }
        }

        return null;
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
            var row = new Grid
            {
                ColumnSpacing = 10,
                HorizontalOptions = LayoutOptions.Fill,
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Star)
                }
            };

            var bullet = new Border
            {
                StrokeThickness = 0,
                BackgroundColor = Color.FromArgb(bubbleBg),
                Padding = new Thickness(7, 3),
                WidthRequest = 26,
                HorizontalOptions = LayoutOptions.Start,
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

            row.Add(bullet, 0);
            row.Add(lbl, 1);
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
            var row = new Grid
            {
                ColumnSpacing = 12,
                HorizontalOptions = LayoutOptions.Fill,
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Star)
                }
            };

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

            row.Add(numBubble, 0);
            row.Add(lbl, 1);
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

        // Pass the exact career currently shown on this details page.
        // CareerRoadmapPage uses this id to select the matching roadmap and
        // render the correct career-specific timeline.
        await Shell.Current.GoToAsync(
            $"{nameof(CareerRoadmapPage)}?careerId={Uri.EscapeDataString(_careerId)}");
    }

    private async void OnViewParentInsightsClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(
            $"{nameof(CareerInsightsPage)}?careerId={Uri.EscapeDataString(_careerId)}");
    }

    private async void OnBackTapped(object? sender, EventArgs e)
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
    private async void OnHomeTapped(object? sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(MyCareerPage));
    }

}