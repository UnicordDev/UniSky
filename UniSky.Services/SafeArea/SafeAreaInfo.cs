using Windows.UI.Xaml;

namespace UniSky.Services;

public record class SafeAreaInfo(bool HasTitleBar, bool IsActive, Thickness Bounds, ElementTheme Theme);
