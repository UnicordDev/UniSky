using System;
using System.Collections.Generic;
using UniSky.Pages;
using UniSky.Services.Navigation;

namespace UniSky.Navigation;

internal class RouteTable : IRouteTable
{
    private readonly Dictionary<string, Type> _pages
        = new(StringComparer.Ordinal);

    public RouteTable()
    {
        Map(RouteKinds.Profile, typeof(ProfilePage));
        Map(RouteKinds.Post, typeof(ThreadPage));
    }

    public void Map(string kind, Type pageType)
    {
        if (string.IsNullOrEmpty(kind))
            throw new ArgumentException("Route kind may not be empty.", nameof(kind));
        if (pageType == null)
            throw new ArgumentNullException(nameof(pageType));

        _pages[kind] = pageType;
    }

    public bool TryResolve(NavigationRoute route, out Type pageType)
    {
        if (route != null)
            return _pages.TryGetValue(route.Kind, out pageType);

        pageType = null;
        return false;
    }

    public bool TryCreateRoute(object payload, out NavigationRoute route)
        => RouteFactory.TryCreateRoute(payload, out route);
}
