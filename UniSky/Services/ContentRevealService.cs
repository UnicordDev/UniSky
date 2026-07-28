using System;
using System.Collections.Generic;
using FishyFlip.Models;

namespace UniSky.Services;

internal sealed class ContentRevealService : IContentRevealService
{
    private readonly HashSet<string> _revealed = new(StringComparer.Ordinal);

    public bool IsRevealed(ATUri uri)
    {
        if (uri == null)
            return false;

        lock (_revealed)
            return _revealed.Contains(uri.ToString());
    }

    public void SetRevealed(ATUri uri, bool revealed)
    {
        if (uri == null)
            return;

        var key = uri.ToString();
        lock (_revealed)
        {
            if (revealed)
                _revealed.Add(key);
            else
                _revealed.Remove(key);
        }
    }
}
