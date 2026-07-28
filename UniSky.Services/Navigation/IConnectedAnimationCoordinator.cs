namespace UniSky.Services.Navigation;

/// <summary>
/// Carries a connected animation across a back navigation, from the page being torn down to the one
/// being restored.
/// </summary>
public interface IConnectedAnimationCoordinator
{
    /// <summary>
    /// Records that a back animation under <paramref name="key"/> has been prepared, and that it came
    /// from <paramref name="origin"/>.
    /// </summary>
    void PrepareBack(string key, NavigationRoute origin);

    /// <summary>
    /// Reads the pending back animation left under <paramref name="key"/>, without claiming it.
    /// </summary>
    bool TryPeekBack(string key, out NavigationRoute origin);

    /// <summary>
    /// Clears the pending entry because it has been landed.
    /// </summary>
    void Complete(string key);

    /// <summary>
    /// Drops any pending back animation and releases the element it froze.
    /// </summary>
    void CancelPending();
}
