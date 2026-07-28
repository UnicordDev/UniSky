using System;
using System.Collections.Generic;
using System.Linq;
using UniSky.Services.Navigation;

namespace UniSky.Navigation;

/// <summary>
/// Per-view registry of navigation scopes.
/// </summary>
internal class NavigationScopeRegistry : INavigationScopeRegistry
{
    private static readonly object ShellsLock = new();
    private static readonly List<INavigationScope> Shells = [];

    private readonly Dictionary<string, INavigationScope> _scopes
        = new(StringComparer.Ordinal);

    public INavigationScope Shell { get; private set; }

    public IReadOnlyList<INavigationScope> AllShells
    {
        get
        {
            lock (ShellsLock)
                return Shells.ToArray();
        }
    }

    public INavigationScope Find(string id)
        => id != null && _scopes.TryGetValue(id, out var scope) ? scope : null;

    public IDisposable Register(INavigationScope scope)
    {
        if (scope == null)
            throw new ArgumentNullException(nameof(scope));

        _scopes[scope.Id] = scope;

        if (scope.Kind == NavigationScopeKind.Shell)
        {
            Shell = scope;
            lock (ShellsLock)
                Shells.Add(scope);
        }

        return new Registration(this, scope);
    }

    private void Unregister(INavigationScope scope)
    {
        if (_scopes.TryGetValue(scope.Id, out var existing) && ReferenceEquals(existing, scope))
            _scopes.Remove(scope.Id);

        if (scope.Kind != NavigationScopeKind.Shell)
            return;

        if (ReferenceEquals(Shell, scope))
            Shell = null;

        lock (ShellsLock)
            Shells.Remove(scope);
    }

    private sealed class Registration : IDisposable
    {
        private NavigationScopeRegistry _registry;
        private readonly INavigationScope _scope;

        public Registration(NavigationScopeRegistry registry, INavigationScope scope)
        {
            _registry = registry;
            _scope = scope;
        }

        public void Dispose()
        {
            var registry = _registry;
            if (registry == null)
                return;

            _registry = null;
            registry.Unregister(_scope);
            _scope.Parent?.RemoveChild(_scope);
        }
    }
}
