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

    protected override async void OnNavigating(ShellNavigatingEventArgs args)
    {
        base.OnNavigating(args);

        // MyCareerPage is a global/pushed route. If a bottom tab is selected
        // while MyCareerPage is on top of the stack, reset to that tab's root
        // instead of leaving the pushed MyCareerPage visible.
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
            await GoToAsync(target);
        }
        finally
        {
            _resettingTopLevelTab = false;
        }
    }
}
