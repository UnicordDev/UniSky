namespace UniSky.Services.Navigation;

/// <summary>
/// Raised by the scope that actually performed a navigation, after the fact.
/// </summary>
public sealed class NavigationCompletedEventArgs
{
    public NavigationCompletedEventArgs(NavigationRequest request, bool success)
    {
        Request = request;
        Success = success;
    }

    public NavigationRequest Request { get; }

    public bool Success { get; }
}
