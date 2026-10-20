namespace StudyMap.Views;

public partial class InformationDisclaimerView : ContentView
{
    public static readonly BindableProperty ModeProperty =
        BindableProperty.Create(
            nameof(Mode),
            typeof(string),
            typeof(InformationDisclaimerView),
            "Compact",
            propertyChanged: OnModeChanged);

    public string Mode
    {
        get => (string)GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    private static void OnModeChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is InformationDisclaimerView view)
            view.ApplyMode();
    }

    public InformationDisclaimerView()
    {
        InitializeComponent();
        ApplyMode();
    }

    private void ApplyMode()
    {
        var prominent = string.Equals(Mode, "Prominent", StringComparison.OrdinalIgnoreCase);
        CompactCard.IsVisible = !prominent;
        ProminentCard.IsVisible = prominent;
    }

    private async void OnDisclaimerTapped(object? sender, TappedEventArgs e)
    {
        var page = Shell.Current?.CurrentPage;
        if (page is null)
            return;

        await page.DisplayAlertAsync(
            "About this information",
            "This information is for educational and career-exploration purposes only. Career paths, salaries, entrance requirements, demand and career opportunities may change over time and vary by location, institution and individual experience. Please verify important details with official sources and consult teachers, parents or qualified professionals before making important education or career decisions.",
            "OK");
    }
}
