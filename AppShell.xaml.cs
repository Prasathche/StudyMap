using Microsoft.Maui.Controls.Shapes;

namespace StudyMap;

public partial class AppShell : Shell
{
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

        // My Career is a pushed page above the current Shell tab. Keep the
        // bottom menu visible, but make all five tabs look unselected while
        // My Career is displayed.
        SetMyCareerTabVisualState(CurrentPage is MyCareerPage);

        // Apply the shared navigation treatment after each page is loaded.
        Dispatcher.Dispatch(() => ApplyNavigationVisuals(CurrentPage));
    }

    private void SetMyCareerTabVisualState(bool myCareerVisible)
    {
        if (myCareerVisible)
        {
            // The Shell still has an internal CurrentItem, but visually all
            // tabs use the same unselected color. This makes it appear that
            // no tab is selected while My Career is on screen.
            Shell.SetTabBarForegroundColor(this, Color.FromArgb("#6F7180"));
            Shell.SetTabBarTitleColor(this, Color.FromArgb("#6F7180"));
            Shell.SetTabBarUnselectedColor(this, Color.FromArgb("#6F7180"));
        }
        else
        {
            // Restore the normal selected/unselected colors for the actual
            // top-level Shell pages.
            Shell.SetTabBarForegroundColor(this, Color.FromArgb("#7C57F5"));
            Shell.SetTabBarTitleColor(this, Color.FromArgb("#7C57F5"));
            Shell.SetTabBarUnselectedColor(this, Color.FromArgb("#6F7180"));
        }
    }

    private static void ApplyNavigationVisuals(Page? page)
    {
        if (page is null)
            return;

        ApplyNavigationVisualsToTree(page, parentIsBorder: false);
    }

    private static void ApplyNavigationVisualsToTree(
        Microsoft.Maui.IVisualTreeElement element,
        bool parentIsBorder)
    {
        foreach (var child in element.GetVisualChildren())
        {
            // Home: replace the existing home glyph with the filled purple
            // home icon shown in the reference, inside a clean white circle.
            if (child is Border homeBorder &&
                homeBorder.Content is Label { Text: "⌂" })
            {
                homeBorder.WidthRequest = 44;
                homeBorder.HeightRequest = 44;
                homeBorder.Padding = 0;
                homeBorder.BackgroundColor = Colors.White;
                homeBorder.Stroke = Colors.Transparent;
                homeBorder.StrokeThickness = 0;
                homeBorder.StrokeShape = new RoundRectangle
                {
                    CornerRadius = new CornerRadius(22)
                };
                homeBorder.Shadow = new Shadow
                {
                    Brush = Color.FromArgb("#18000000"),
                    Offset = new Point(0, 3),
                    Radius = 8,
                    Opacity = 1
                };

                homeBorder.Content = new Image
                {
                    Source = "nav_home.svg",
                    WidthRequest = 26,
                    HeightRequest = 26,
                    Aspect = Aspect.AspectFit,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                };

                SemanticProperties.SetDescription(homeBorder, "My Career");
            }

            // Back controls already wrapped in a Border should use that
            // existing circle. Only the icon itself is replaced/styled.
            if (child is Border backBorder &&
                backBorder.Content is ImageButton backImageButton &&
                IsBackButton(backImageButton))
            {
                backBorder.WidthRequest = 44;
                backBorder.HeightRequest = 44;
                backBorder.Padding = 0;
                backBorder.BackgroundColor = Colors.White;
                backBorder.Stroke = Colors.Transparent;
                backBorder.StrokeThickness = 0;
                backBorder.StrokeShape = new RoundRectangle
                {
                    CornerRadius = new CornerRadius(22)
                };
                backBorder.Shadow = new Shadow
                {
                    Brush = Color.FromArgb("#18000000"),
                    Offset = new Point(0, 3),
                    Radius = 8,
                    Opacity = 1
                };

                backImageButton.Source = "nav_back.svg";
                backImageButton.WidthRequest = 32;
                backImageButton.HeightRequest = 32;
                backImageButton.Padding = 5;
                backImageButton.BackgroundColor = Colors.Transparent;
                backImageButton.BorderWidth = 0;
                backImageButton.CornerRadius = 0;
                SemanticProperties.SetDescription(backBorder, "Back");
            }

            // Direct back buttons (without a Border wrapper) get the same
            // 44px white circular treatment as the reference design.
            if (child is ImageButton directBackButton &&
                !parentIsBorder &&
                IsBackButton(directBackButton))
            {
                directBackButton.Source = "nav_back.svg";
                directBackButton.WidthRequest = 44;
                directBackButton.HeightRequest = 44;
                directBackButton.Padding = 5;
                directBackButton.BackgroundColor = Colors.White;
                directBackButton.BorderColor = Colors.Transparent;
                directBackButton.BorderWidth = 0;
                directBackButton.CornerRadius = 22;
                SemanticProperties.SetDescription(directBackButton, "Back");
            }

            ApplyNavigationVisualsToTree(child, child is Border);
        }
    }

    private static bool IsBackButton(ImageButton button)
    {
        return button.Source is FileImageSource fileSource &&
               string.Equals(fileSource.File, "arrow_back.png", StringComparison.OrdinalIgnoreCase);
    }
}
