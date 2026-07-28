using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Animation;

namespace UniSky.Services.Navigation;

/// <summary>
/// A request to go somewhere. Raised against the scope the originating viewmodel belongs to, then
/// bubbled until something handles it.
/// </summary>
public sealed class NavigationRequest(NavigationRoute route)
{
    /// <summary>
    /// The destination.
    /// </summary>
    public NavigationRoute Route { get; } = route ?? throw new ArgumentNullException(nameof(route));

    /// <summary>
    /// An existing model for the destination, saves a refetch.
    /// </summary>
    public object Payload { get; init; }

    public NavigationIntent Intent { get; init; } = NavigationIntent.Default;

    public NavigationSource Source { get; init; } = NavigationSource.User;

    public NavigationTransitionInfo Transition { get; init; }

    public ConnectedAnimationRequest Animation { get; init; }

    public string TargetScope { get; init; }

    /// <summary>
    /// A copy of this request without anything bound to the current CoreWindow
    /// </summary>
    public NavigationRequest ForCrossThread()
        => new(Route)
        {
            Intent = Intent,
            Source = Source,
            TargetScope = TargetScope
        };

    /// <summary>
    /// A copy of this request with a different intent, for handlers that redirect rather than handle.
    /// </summary>
    public NavigationRequest WithIntent(NavigationIntent intent)
        => new(Route)
        {
            Payload = Payload,
            Intent = intent,
            Source = Source,
            Transition = Transition,
            Animation = Animation,
            TargetScope = TargetScope
        };

    /// <summary>
    /// A copy of this request carrying a transition override.
    /// </summary>
    public NavigationRequest WithTransition(NavigationTransitionInfo transition)
        => new(Route)
        {
            Payload = Payload,
            Intent = Intent,
            Source = Source,
            Transition = transition,
            Animation = Animation,
            TargetScope = TargetScope
        };

    /// <summary>
    /// A copy of this request carrying a connected animation, or this request unchanged when there is
    /// no source element to fly.
    /// </summary>
    public NavigationRequest WithAnimation(string key, UIElement source, params UIElement[] coordinated)
    {
        if (source == null)
            return this;
            
        return new NavigationRequest(Route)
        {
            Payload = Payload,
            Intent = Intent,
            Source = Source,
            Transition = Transition,
            Animation = new ConnectedAnimationRequest(key, source)
            {
                Coordinated = coordinated is { Length: > 0 } ? coordinated : null
            },
            TargetScope = TargetScope
        };
    }


    /// <summary>
    /// A copy of this request aimed at a named scope, bypassing bubbling.
    /// </summary>
    public NavigationRequest WithTarget(string scopeId)
        => new(Route)
        {
            Payload = Payload,
            Intent = Intent,
            Source = Source,
            Transition = Transition,
            Animation = Animation,
            TargetScope = scopeId
        };

    public override string ToString()
        => $"{Route.ToCanonicalString()} ({Intent}, {Source})";
}
