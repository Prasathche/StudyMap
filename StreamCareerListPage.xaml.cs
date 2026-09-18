using StudyMap.Models;
using StudyMap.ViewModels;

namespace StudyMap;

[QueryProperty(nameof(StreamName), "streamName")]
public partial class StreamCareerListPage : ContentPage
{
    private readonly StreamCareersViewModel _vm;
    private string _streamName = string.Empty;

    public StreamCareerListPage()
    {
        InitializeComponent();
        _vm = new StreamCareersViewModel();
        BindingContext = _vm;
        BuildFilterChips();
    }

    // ── QueryProperty ────────────────────────────────────────────────
    public string StreamName
    {
        get => _streamName;
        set
        {
            _streamName = Uri.UnescapeDataString(value ?? string.Empty);
            MainThread.BeginInvokeOnMainThread(async () =>
                await LoadStreamAsync(_streamName));
        }
    }

    // ── Lifecycle ────────────────────────────────────────────────────
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.RefreshFavoriteStates();
        UpdateResultsLabel();
    }
private async void OnHomeTapped(object? sender, TappedEventArgs e)
{
    await Shell.Current.GoToAsync(nameof(MyCareerPage));
}
    // ── Data loading ─────────────────────────────────────────────────
    private async Task LoadStreamAsync(string streamName)
    {
        if (string.IsNullOrWhiteSpace(streamName)) return;

        ShowLoading(true);
        ApplyStreamTheme(streamName);
        UpdateHeaderLabels(streamName);

        await _vm.LoadCareersForStreamAsync(streamName);
        CareersCV.ItemsSource = _vm.StreamCareers;

        ShowLoading(false);
        UpdateResultsLabel();
        UpdateCareerCountBadge();
    }

    // ── Filter chips ──────────────────────────────────────────────────
    private void BuildFilterChips()
    {
        var chips = new[] { "All", "Popular", "Favorite", "High Salary" };
        FilterChipsLayout.Children.Clear();

        foreach (var chip in chips)
        {
            var btn = new Button
            {
                Text = chip,
                CornerRadius = 14,
                Padding = new Thickness(16, 8),
                FontFamily = "OpenSansSemibold",
                FontSize = 13,
                MinimumHeightRequest = 0,
                MinimumWidthRequest = 0
            };
            SetChipStyle(btn, chip == "All");
            btn.Clicked += (s, e) => OnFilterChipClicked(btn, chip);
            FilterChipsLayout.Children.Add(btn);
        }
    }

    private void OnFilterChipClicked(Button tapped, string chip)
    {
        foreach (var child in FilterChipsLayout.Children)
            if (child is Button btn)
                SetChipStyle(btn, btn == tapped);

        _vm.ActiveFilter = chip;
        UpdateResultsLabel();
    }

    private static void SetChipStyle(Button btn, bool selected)
    {
        btn.BackgroundColor = selected
            ? Color.FromArgb("#17203B")
            : Colors.White;
        btn.TextColor = selected
            ? Colors.White
            : Color.FromArgb("#7D849A");
    }

    // ── Search ────────────────────────────────────────────────────────
    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        _vm.SearchQuery = e.NewTextValue ?? string.Empty;
        UpdateResultsLabel();
    }

    // ── Navigation ────────────────────────────────────────────────────
    private async void OnBackTapped(object? sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void OnCareerTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is Career career)
            await NavigateToDetail(career);
    }

    private async void OnViewDetailsTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is Career career)
            await NavigateToDetail(career);
    }

    private async Task NavigateToDetail(Career career)
    {
        await Shell.Current.GoToAsync(
            $"{nameof(CareerDetailPage)}?careerId={Uri.EscapeDataString(career.Id)}");
    }

    // ── UI helpers ────────────────────────────────────────────────────
    private void UpdateHeaderLabels(string streamName)
    {
        StreamTitleLabel.Text    = $"{streamName} Careers";
        StreamSubtitleLabel.Text = $"Explore all {streamName.ToLower()} career options";
    }

    private void UpdateResultsLabel()
    {
        var count = _vm.StreamCareers.Count;
        ResultsLabel.Text = count == 0
            ? "No careers match your filter"
            : $"Showing {count} career{(count == 1 ? "" : "s")}";
    }

    private void UpdateCareerCountBadge()
    {
        var count = _vm.StreamCareers.Count;
        CareerCountBadge.Text = $"{count}";
    }

    private void ApplyStreamTheme(string streamName)
    {
        switch (streamName.ToLowerInvariant())
        {
            case "science":
                StreamIconLabel.Text  = "\U0001f52c";
                BannerIcon.Text       = "\U0001f52c";
                BannerTitle.Text      = "Science Stream";
                BannerDesc.Text       = "Technology, Healthcare & Engineering";
                SetGradient(HdrGradA,    HdrGradB,    "#E3F2FD", "#BBDEFB");
                SetGradient(BannerGradA, BannerGradB, "#E3F2FD", "#BBDEFB");
                break;

            case "commerce":
                StreamIconLabel.Text  = "\U0001f4c8";
                BannerIcon.Text       = "\U0001f4c8";
                BannerTitle.Text      = "Commerce Stream";
                BannerDesc.Text       = "Business, Finance & Economics";
                SetGradient(HdrGradA,    HdrGradB,    "#E8F5E9", "#C8E6C9");
                SetGradient(BannerGradA, BannerGradB, "#E8F5E9", "#C8E6C9");
                break;

            case "arts":
                StreamIconLabel.Text  = "\U0001f3a8";
                BannerIcon.Text       = "\U0001f3a8";
                BannerTitle.Text      = "Arts Stream";
                BannerDesc.Text       = "Design, Media & Social Sciences";
                SetGradient(HdrGradA,    HdrGradB,    "#F3E5F5", "#E1BEE7");
                SetGradient(BannerGradA, BannerGradB, "#F3E5F5", "#E1BEE7");
                break;
        }
    }

    private static void SetGradient(GradientStop a, GradientStop b, string start, string end)
    {
        a.Color = Color.FromArgb(start);
        b.Color = Color.FromArgb(end);
    }

    private void ShowLoading(bool show)
    {
        LoadingOverlay.IsVisible = show;
        CareersCV.IsVisible      = !show;
    }
}