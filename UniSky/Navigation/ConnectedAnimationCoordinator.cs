using UniSky.Services.Navigation;

namespace UniSky.Navigation;

/// <inheritdoc cref="IConnectedAnimationCoordinator"/>
internal sealed class ConnectedAnimationCoordinator : IConnectedAnimationCoordinator
{
    private string _key;
    private NavigationRoute _origin;

    public void PrepareBack(string key, NavigationRoute origin)
    {
        if (string.IsNullOrEmpty(key) || origin == null || !ConnectedAnimations.IsEnabled)
            return;

        if (_key != null && _key != key)
            ConnectedAnimations.Cancel(_key);

        _key = key;
        _origin = origin;
    }

    public bool TryPeekBack(string key, out NavigationRoute origin)
    {
        if (string.IsNullOrEmpty(key) || _key != key)
        {
            origin = null;
            return false;
        }

        origin = _origin;
        return true;
    }

    public void Complete(string key)
    {
        if (_key != key)
            return;

        _key = null;
        _origin = null;
    }

    public void CancelPending()
    {
        if (_key == null)
            return;

        ConnectedAnimations.Cancel(_key);
        _key = null;
        _origin = null;
    }
}
