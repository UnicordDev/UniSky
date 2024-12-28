using System;

namespace UniSky.Services;

public class SafeAreaUpdatedEventArgs : EventArgs
{
    public SafeAreaInfo SafeArea { get; init; }
}
