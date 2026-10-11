namespace StudyMap;

public partial class AppShell : Shell
{
    private bool _resettingTopLevelTab;

    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(MyCareerPage), typeof(MyCareerPage));

        // Modal / pushed routes for stream-based career flow
        Routing.RegisterRoute(nameof(StreamCareerListPage), typeof(StreamCareerListPage));
        Routing.RegisterRoute(nameof(CareerDetailPage), typeof(CareerDetailPage));
        Routing.RegisterRoute(nameof(CareerRoadmapPage), typeof(CareerRoadmapPage));
        Routing.RegisterRoute(nameof(CareerStageDetailsPage), typeof(CareerStageDetailsPage));

        // Pushed routes for the "Not Sure What to Choose" career discovery flow
        Routing.RegisterRoute(nameof(CareerQuestionPage), typeof(CareerQuestionPage));
        Routing.RegisterRoute(nameof(CareerResultsPage), typeof(CareerResultsPage));

        // Pushed routes for the Parent Insights flow
        Routing.RegisterRoute(nameof(CareerInsightsPage), typeof(CareerInsightsPage));
        Routing.RegisterRoute(nameof(CareerComparisonPage), typeof(CareerComparisonPage));
    }

    protected override void OnNavigated(ShellNavigatedEventArgs args)
    {
        base.OnNavigated(args);

        // Apply the shared navigation treatment after each page is loaded.
        // This keeps the existing navigation handlers and routes unchanged.
        Dispatcher.Dispatch(() => ApplyNavigationVisuals(CurrentPage));
    }

    private static void ApplyNavigationVisuals(Page? page)
    {
        if (page is null)
            return;

        ApplyNavigationVisualsToTree(page);
    }

    private static void ApplyNavigationVisualsToTree(Microsoft.Maui.IVisualTreeElement element)
    {
        foreach (var child in element.GetVisualChildren())
        {
            // My Career: convert the existing home icon into the selected
            // design — a compact pill containing the home icon and label.
            if (child is Border homeBorder &&
                homeBorder.Content is Label { Text: "⌂" })
            {
                homeBorder.WidthRequest = 118;
                homeBorder.HeightRequest = 46;
                homeBorder.Padding = new Thickness(10, 0);
                homeBorder.BackgroundColor = Color.FromArgb("#F8F5FF");
                homeBorder.Stroke = Color.FromArgb("#E6D9FF");
                homeBorder.StrokeThickness = 1;
                homeBorder.StrokeShape = new RoundRectangle
                {
                    CornerRadius = new CornerRadius(23)
                };
                homeBorder.Shadow = new Shadow
                {
                    Brush = Color.FromArgb("#18000000"),
                    Offset = new Point(0, 3),
                    Radius = 8,
                    Opacity = 1
                };

                var navigationGrid = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitionCollection
                    {
                        new ColumnDefinition(GridLength.Auto),
                        new ColumnDefinition(GridLength.Star)
                    },
                    ColumnSpacing = 7,
                    VerticalOptions = LayoutOptions.Center
                };

                navigationGrid.Add(new Label
                {
                    Text = "⌂",
                    FontSize = 22,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#7C57F5"),
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                });

                navigationGrid.Add(new Label
                {
                    Text = "My Career",
                    FontFamily = "OpenSansSemibold",
                    FontSize = 12,
                    TextColor = Color.FromArgb("#4E3AA8"),
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Start
                }, 1);

                homeBorder.Content = navigationGrid;
                SemanticProperties.SetDescription(homeBorder, "My Career");
            }

            // Back buttons: make every arrow_back.png button a consistent,
            // touch-friendly circular control. Existing Border wrappers are
            // preserved, so pages that already have one keep their layout.
            if (child is ImageButton backButton &&
                backButton.Source is FileImageSource fileSource &&
                string.Equals(fileSource.File, "arrow_back.png", StringComparison.OrdinalIgnoreCase))
            {
                backButton.WidthRequest = 44;
                backButton.HeightRequest = 44;
                backButton.Padding = 9;
                backButton.BackgroundColor = Colors.White;
                backButton.BorderColor = Color.FromArgb("#E6D9FF");
                backButton.BorderWidth = 1;
                backButton.CornerRadius = 22;
                SemanticProperties.SetDescription(backButton, "Back");
            }

            ApplyNavigationVisualsToTree(child);
        }
    }

    protected override async void OnNavigating(ShellNavigatingEventArgs args)
    {
        base.OnNavigating(args);

        // MyCareerPage is a pushed route. When a bottom tab is selected
        // while MyCareerPage is on top, remove the pushed page first so
        // the requested top-level tab becomes visible.
        if (_resettingTopLevelTab ||
            args.Source is not (ShellNavigationSource.ShellItemChanged
                or ShellNavigationSource.ShellSectionChanged
                or ShellNavigationSource.ShellContentChanged) ||
            !args.Target.Location.ToString().StartsWith("//", StringComparison.Ordinal))
        {
            return;
        }

        if (CurrentPage is not MyCareerPage)
        {
            return;
        }

        var target = args.Target.Location.ToString();
        args.Cancel();

        _resettingTopLevelTab = true;
        try
        {
            await Navigation.PopAsync();
            await GoToAsync(target);
        }
        finally
        {
            _resettingTopLevelTab = false;
        }
    }
}
