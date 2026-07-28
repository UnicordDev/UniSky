using Microsoft.Extensions.DependencyInjection;

namespace UniSky.Services.Navigation;

/// <summary>
/// Convenience access to the ambient navigation scope of the calling view.
/// </summary>
public static class NavigationScopes
{
    /// <summary>
    /// The shell scope of the view running on this thread, or <see langword="null"/> if none has
    /// registered yet. You probably don't want to use this, there's usually a more specific scope.
    /// </summary>
    public static INavigationScope ShellForCurrentView
        => ServiceContainer.Scoped.GetService<INavigationScopeRegistry>()?.Shell;
}
