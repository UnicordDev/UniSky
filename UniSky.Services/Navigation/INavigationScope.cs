using System.Collections.Generic;
using Windows.Foundation;

namespace UniSky.Services.Navigation;

/// <summary>
/// A node in the navigation tree.
/// </summary>
public interface INavigationScope : INavigationContext
{
    /// <summary>
    /// Identifies this scope within its view, e.g. <c>root</c>, <c>home</c>, <c>home:Feeds</c>,
    /// <c>sheet:2</c>, <c>column:1</c>.
    /// </summary>
    string Id { get; }

    NavigationScopeKind Kind { get; }

    NavigationScopePolicy Policy { get; }

    INavigationScope Parent { get; }

    IReadOnlyList<INavigationScope> Children { get; }

    INavigationScope ActiveChild { get; set; }

    bool Supports(NavigationIntent intent);

    /// <summary>
    /// Performs the navigation here and now, without bubbling. Called by whoever claimed the
    /// request; use <see cref="INavigationContext.Navigate"/> to enter the chain instead.
    /// </summary>
    bool Handle(NavigationRequest request);

    void AddChild(INavigationScope child);

    void RemoveChild(INavigationScope child);

    IReadOnlyList<string> GetSerializedState();

    event TypedEventHandler<INavigationScope, NavigationRequestedEventArgs> Requesting;

    event TypedEventHandler<INavigationScope, NavigationCompletedEventArgs> Navigated;
}
