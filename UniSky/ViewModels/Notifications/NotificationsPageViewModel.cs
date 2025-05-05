using System.Threading.Tasks;

namespace UniSky.ViewModels.Notifications;

public partial class NotificationsPageViewModel : ViewModelBase
{
    public NotificationsCollection Notifications { get; }

    public NotificationsPageViewModel()
    {
        this.Notifications = new NotificationsCollection(this);
    }

    public async Task RefreshAsync()
    {
        await Notifications.RefreshAsync();
    }
}
