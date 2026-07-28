namespace UniSky.Services.Navigation;

/// <summary>
/// Something a user can navigate to, which can say where it is.
/// </summary>
public interface IRoutable
{
    /// <summary>
    /// Where this item lives, or <see langword="null"/> if it has no addressable route.
    /// </summary>
    NavigationRoute? Route { get; }
}
