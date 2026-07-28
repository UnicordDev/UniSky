namespace UniSky.Services.Navigation;

/// <summary>
/// Holds navigation requests that arrive from outside the app until there is somewhere to send them.
/// </summary>
public interface IActivationCoordinator
{
    void Enqueue(NavigationRequest request);

    /// <summary>
    /// Nominates where queued requests should go, flushing anything already waiting. Passing
    /// <see langword="null"/> detaches.
    /// </summary>
    void SetTarget(INavigationContext target);
}
