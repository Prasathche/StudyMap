namespace StudyMap;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(MyCareerPage), typeof(MyCareerPage));

        // Modal / pushed routes for stream-based career flow
        Routing.RegisterRoute(nameof(StreamCareerListPage), typeof(StreamCareerListPage));
        Routing.RegisterRoute(nameof(CareerDetailPage),     typeof(CareerDetailPage));
        Routing.RegisterRoute(nameof(CareerRoadmapPage),    typeof(CareerRoadmapPage));
        Routing.RegisterRoute(nameof(CareerStageDetailsPage), typeof(CareerStageDetailsPage));

        // Pushed routes for the "Not Sure What to Choose" career discovery flow
        Routing.RegisterRoute(nameof(CareerQuestionPage),  typeof(CareerQuestionPage));
        Routing.RegisterRoute(nameof(CareerResultsPage),   typeof(CareerResultsPage));

        // Pushed routes for the Parent Insights flow
        Routing.RegisterRoute(nameof(CareerInsightsPage),   typeof(CareerInsightsPage));
        Routing.RegisterRoute(nameof(CareerComparisonPage), typeof(CareerComparisonPage));
    }
}