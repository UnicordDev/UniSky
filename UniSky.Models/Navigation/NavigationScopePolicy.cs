namespace UniSky.Services.Navigation;

public enum NavigationScopePolicy
{
    /// <summary>
    /// Handle what this scope can, pass the rest up. The default.
    /// </summary>
    BubbleUnknown,
    /// <summary>
    /// Handle everything here, even routes this scope has no page for, sheets use this.
    /// </summary>
    Contain,
    /// <summary>
    /// Handle nothing, always defer to the parent. Useful for a scope that exists only to be a
    /// back-navigation participant, think overlays.
    /// </summary>
    BubbleAll
}
