using UniSky.Services.Navigation;
using Windows.UI.Xaml.Controls;

namespace UniSky.Navigation;

/// <summary>
/// Helpers for sheets that host their own navigable frame.
/// </summary>
public static class SheetNavigationScope
{
    /// <summary>
    /// Gives <paramref name="frame"/> a content scope nested inside the sheet hosting it.
    /// </summary>
    public static INavigationScope Attach(Frame frame, Control sheet)
    {
        var host = NavigationScopeHost.FindFor(sheet);
        var id = host != null ? $"{host.Id}:content" : "sheet:content";

        NavigationScopeHost.SetKind(frame, NavigationScopeKind.Content);
        NavigationScopeHost.SetScope(frame, id);
        return NavigationScopeHost.EnsureScope(frame);
    }
}
