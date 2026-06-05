using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Loadout.App.Behaviors;

/// <summary>
/// Attached behavior that animates a <see cref="ProgressBar"/>'s value instead of
/// snapping to it. Bind <c>SmoothProgress.TargetValue</c> to the live metric: each
/// update glides to the new value over a short duration, so the gauge moves
/// fluidly instead of ticking once per refresh.
/// </summary>
public static class SmoothProgress
{
    public static readonly DependencyProperty TargetValueProperty =
        DependencyProperty.RegisterAttached(
            "TargetValue", typeof(double), typeof(SmoothProgress),
            new PropertyMetadata(0.0, OnTargetValueChanged));

    public static double GetTargetValue(DependencyObject o) => (double)o.GetValue(TargetValueProperty);
    public static void SetTargetValue(DependencyObject o, double value) => o.SetValue(TargetValueProperty, value);

    private static void OnTargetValueChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
    {
        if (o is not ProgressBar bar) return;

        double to = (double)e.NewValue;
        // Slightly shorter than the 1s sensor refresh so the gauge reaches its
        // target just as the next reading arrives — continuous, never stuttering.
        var animation = new DoubleAnimation(to, new Duration(TimeSpan.FromMilliseconds(900)))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        bar.BeginAnimation(ProgressBar.ValueProperty, animation);
    }
}
