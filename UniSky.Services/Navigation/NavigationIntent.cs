namespace UniSky.Services.Navigation;

/// <summary>
/// How a <see cref="NavigationRequest"/> wants to be presented. 
/// </summary>
public enum NavigationIntent
{
    Default,
    Replace,
    Root,
    NewColumn,
    NewWindow,
    Sheet
}
