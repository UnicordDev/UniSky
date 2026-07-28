using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace UniSky.Services.Navigation;

/// <summary>
/// A request to fly an element from the current view into the destination.
/// </summary>
public sealed class ConnectedAnimationRequest
{
    public ConnectedAnimationRequest(string key, UIElement source)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("An animation key is required.", nameof(key));

        Key = key;
        Source = source ?? throw new ArgumentNullException(nameof(source));
        Origin = FindOrigin(source);
    }

    /// <summary>
    /// Walks up from the source element to the list row it sits in, and takes that row's route.
    /// </summary>
    private static NavigationRoute FindOrigin(UIElement source)
    {
        for (DependencyObject current = source; current != null; current = VisualTreeHelper.GetParent(current))
        {
            if (current is FrameworkElement { DataContext: IRoutable routable } && routable.Route != null)
                return routable.Route;
        }

        return null;
    }
    
    public string Key { get; }

    public UIElement Source { get; }

    /// <summary>
    /// The list row the source element came from, or null if it wasn't in one.
    /// </summary>
    public NavigationRoute Origin { get; }
    
    public IReadOnlyList<UIElement> Coordinated { get; init; }
}
