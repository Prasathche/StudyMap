using Android.App;
using Android.Content.PM;
using Android.OS;

namespace StudyMap;

// Use the normal MAUI app theme so Android 12+ owns the launch splash screen.
// This avoids the additional MAUI SplashTheme screen appearing after the
// Android system splash and before the first StudyMap page.
[Activity(Theme = "@style/Maui.MainTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
}
