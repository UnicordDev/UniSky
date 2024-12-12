using UniSky.Services;

namespace UniSky.ViewModels.Notifications;

public partial class NotificationsPageViewModel : ViewModelBase
{
    private IProtocolService protocolService;

    public NotificationsCollection Notifications { get; }

    public NotificationsPageViewModel(IProtocolService protocolService)
    {
        this.protocolService = protocolService;
        this.Notifications = new NotificationsCollection(this, protocolService);
    }
}
