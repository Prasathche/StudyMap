#if ANDROID

using Android.Content;
using Android.Content.Res;
using Android.Graphics.Drawables;
using Android.Views;
using Android.Widget;
using Google.Android.Material.BottomNavigation;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;

namespace StudyMap.Platforms.Android;

public class StudyMapShellRenderer : ShellRenderer
{
    public StudyMapShellRenderer(Context context)
        : base(context)
    {
    }

    protected override IShellBottomNavViewAppearanceTracker
        CreateBottomNavViewAppearanceTracker(ShellItem shellItem)
    {
        return new StudyMapBottomNavAppearanceTracker(
            this,
            shellItem);
    }
}

public class StudyMapBottomNavAppearanceTracker
    : ShellBottomNavViewAppearanceTracker
{
    public StudyMapBottomNavAppearanceTracker(
        IShellContext shellContext,
        ShellItem shellItem)
        : base(shellContext, shellItem)
    {
    }

    public override void SetAppearance(
        BottomNavigationView bottomView,
        IShellAppearanceElement appearance)
    {
        // Let MAUI apply its normal Shell appearance first.
        base.SetAppearance(bottomView, appearance);

        // Convert dp to pixels.
        float density =
            bottomView.Resources?.DisplayMetrics?.Density ?? 1f;

        int Dp(float value)
        {
            return (int)(value * density + 0.5f);
        }

        // Remove the default Material active indicator.
        bottomView.ItemActiveIndicatorEnabled = false;

        // Remove the gray touch ripple.
        bottomView.ItemRippleColor =
            ColorStateList.ValueOf(
                global::Android.Graphics.Color.Transparent);

        // White navigation background.
        bottomView.SetBackgroundColor(
            global::Android.Graphics.Color.White);

        // Create the rounded white card.
        var background = new GradientDrawable();

        background.SetColor(
            global::Android.Graphics.Color.White);

        background.SetCornerRadius(Dp(18));

        background.SetStroke(
            Dp(1),
            global::Android.Graphics.Color.Rgb(226, 224, 234));

        bottomView.Background = background;

        // Subtle shadow.
        bottomView.Elevation = Dp(4);

        // Give the navigation bar space around itself.
        if (bottomView.LayoutParameters
            is ViewGroup.MarginLayoutParams margins)
        {
            margins.LeftMargin = Dp(10);
            margins.RightMargin = Dp(10);
            margins.BottomMargin = Dp(8);
            margins.TopMargin = Dp(4);

            bottomView.LayoutParameters = margins;
        }

        // Target height: approximately 72dp.
        if (bottomView.LayoutParameters != null)
        {
            bottomView.LayoutParameters.Height = Dp(72);
        }

        // Reduce the gap between the icon and the label.
        ReduceIconLabelGap(bottomView, Dp(2));
    }

    private static void ReduceIconLabelGap(
        BottomNavigationView bottomView,
        int extraAdjustment)
    {
        for (int i = 0; i < bottomView.ChildCount; i++)
        {
            var child = bottomView.GetChildAt(i);

            if (child is ViewGroup menuView)
            {
                AdjustMenuView(menuView, extraAdjustment);
            }
        }
    }

    private static void AdjustMenuView(
    ViewGroup parent,
    int adjustment)
{
    for (int i = 0; i < parent.ChildCount; i++)
    {
        var child = parent.GetChildAt(i);

        if (child is TextView textView)
        {
            // Move the label upward to reduce the
            // visual gap between icon and text.
            textView.TranslationY = -DpValue(
                textView,
                6);
        }

        if (child is ViewGroup childGroup)
        {
            AdjustMenuView(childGroup, adjustment);
        }
    }
}

private static float DpValue(
    global::Android.Views.View view,
    float dp)
{
    float density =
        view.Resources?.DisplayMetrics?.Density ?? 1f;

    return dp * density;
}
}

#endif