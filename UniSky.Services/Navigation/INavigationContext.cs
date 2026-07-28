namespace UniSky.Services.Navigation;

public interface INavigationContext
{
    /// <summary>
    /// Raises a request, which bubbles until a scope handles it.
    /// </summary>
    /// <returns><see langword="true"/> if something handled it.</returns>
    bool Navigate(NavigationRequest request);

    bool CanGoBack { get; }

    bool GoBack();
}
