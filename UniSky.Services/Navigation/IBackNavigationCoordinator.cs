using System;

namespace UniSky.Services.Navigation;

/// <summary>
/// The single source of truth of back navigation in a view.
/// </summary>
public interface IBackNavigationCoordinator
{
    IDisposable Register(IBackHandler handler, int priority);

    bool CanGoBack { get; }

    bool TryGoBack();
    void Invalidate();

    event EventHandler CanGoBackChanged;
}

public static class BackPriority
{
    public const int OverlayWindow = 1000;
    public const int Sheet = 900;
    public const int Overlay = 800;
    public const int Navigation = 100;
}
