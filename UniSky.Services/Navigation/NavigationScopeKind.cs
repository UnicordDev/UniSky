namespace UniSky.Services.Navigation;

/// <summary>
/// Defines a scope type which controls what requestst that scope can handle, and how it 
/// acts in the back stack
/// </summary>
public enum NavigationScopeKind
{
    Shell,
    Region,
    Column,
    Sheet,
    Content
}
