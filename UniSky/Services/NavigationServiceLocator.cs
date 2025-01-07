using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace UniSky.Services;

internal class NavigationServiceLocator : INavigationServiceLocator
{
    private readonly Dictionary<string, NavigationService> _services
        = [];

    public INavigationService GetNavigationService(string name)
    {
        if (_services.TryGetValue(name, out var service))
            return service;

        service = ActivatorUtilities.CreateInstance<NavigationService>(ServiceContainer.Scoped);
        _services.Add(name, service);

        return service;
    }
}
