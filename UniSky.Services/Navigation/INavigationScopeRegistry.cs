using System;
using System.Collections.Generic;

namespace UniSky.Services.Navigation;

/// <summary>
/// Tracks the navigation scopes belonging to a view.
/// </summary>
public interface INavigationScopeRegistry
{
    /// <summary>
    /// This view's root scope. The fallback for any request nothing else claimed.
    /// </summary>
    INavigationScope Shell { get; }

    INavigationScope Find(string id);

    /// <summary>
    /// Every view's shell, across all UI threads. Requests sent between dispatchers must be
    /// stripped via <see cref="NavigationRequest.ForCrossThread"/>.
    /// </summary>
    IReadOnlyList<INavigationScope> AllShells { get; }

    /// <summary>
    /// Registers a scope for the lifetime of the returned token.
    /// </summary>
    IDisposable Register(INavigationScope scope);
}
