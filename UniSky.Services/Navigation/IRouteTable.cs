using System;

namespace UniSky.Services.Navigation;

/// <summary>
/// Maps between routes and the pages that render them, in both directions.
/// </summary>
public interface IRouteTable
{
    void Map(string kind, Type pageType);

    bool TryResolve(NavigationRoute route, out Type pageType);
    
    bool TryCreateRoute(object payload, out NavigationRoute route);
}
