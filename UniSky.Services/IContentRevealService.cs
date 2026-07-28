using FishyFlip.Models;

namespace UniSky.Services;

/// <summary>
/// Remembers which content warnings the user has chosen to reveal.
/// </summary>
public interface IContentRevealService
{
    bool IsRevealed(ATUri uri);
    void SetRevealed(ATUri uri, bool revealed);
}
