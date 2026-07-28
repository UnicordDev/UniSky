namespace UniSky.Services.Navigation;

/// <summary>
/// What caused a <see cref="NavigationRequest"/>. Handlers use this to decide whether a request may
/// be animated, skipped, or trusted to carry a hydrated payload.
/// </summary>
public enum NavigationSource
{
    User,
    Protocol,
    Toast,
    Restore,
    Back
}
