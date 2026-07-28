using UniSky.Services.Navigation;

namespace UniSky.Navigation;

internal sealed class NavigationBackHandler(INavigationScope root) : IBackHandler
{
    public bool CanGoBack
        => root?.CanGoBack ?? false;

    public bool TryGoBack()
        => root?.GoBack() ?? false;
}
