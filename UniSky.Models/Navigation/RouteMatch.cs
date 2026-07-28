using System.Collections;
using System.Linq;

namespace UniSky.Services.Navigation;

/// <summary>
/// Locating a destination inside a list of items.
/// </summary>
public static class RouteMatch
{
    /// <summary>
    /// The first item in <paramref name="items"/> that represents <paramref name="route"/>, or
    /// <see langword="null"/>.
    /// </summary>
    public static object? Find(IEnumerable? items, NavigationRoute? route)
        => route is null
            ? null
            : items?.OfType<IRoutable>().FirstOrDefault(x => route.Equals(x.Route));
}
