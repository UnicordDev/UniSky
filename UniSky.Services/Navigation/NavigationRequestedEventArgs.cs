namespace UniSky.Services.Navigation;

public sealed class NavigationRequestedEventArgs
{
    public NavigationRequestedEventArgs(NavigationRequest request)
    {
        Request = request;
    }
    
    public NavigationRequest Request { get; set; }
    
    public bool Handled { get; set; }
    
    public bool Cancel { get; set; }
}
