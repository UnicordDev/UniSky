using System.Threading.Tasks;
using UniSky.Services.Navigation;

namespace UniSky.ViewModels.Notifications;

public partial class NotificationsPageViewModel : ViewModelBase
{
    public NotificationsCollection Notifications { get; }

    public NotificationsPageViewModel(INavigationContext navigation)
        : base(navigation)
    {
        this.Notifications = new NotificationsCollection(this);
    }

    public async Task RefreshAsync()
    {
        await Notifications.RefreshAsync();
    }
}
