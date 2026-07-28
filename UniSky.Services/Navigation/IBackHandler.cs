namespace UniSky.Services.Navigation;

/// <summary>
/// Something that can consume a back gesture.
/// </summary>
public interface IBackHandler
{
    bool CanGoBack { get; }

    /// <summary>
    /// Consumes the back gesture.
    /// </summary>
    bool TryGoBack();
}
