using System;
using System.Threading.Tasks;
using FishyFlip;
using FishyFlip.Lexicon.App.Bsky.Notification;
using FishyFlip.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UniSky.Extensions;
using UniSky.Pages;
using UniSky.Services;
using Windows.UI.Xaml;

namespace UniSky.ViewModels;

public partial class NotificationsMenuItemViewModel : MenuItemViewModel
{
    private readonly DispatcherTimer notificationUpdateTimer;

    private readonly IBadgeService badgeService
        = ServiceContainer.Scoped.GetRequiredService<IBadgeService>();
    private readonly IProtocolService protocolService
        = ServiceContainer.Scoped.GetRequiredService<IProtocolService>();
    private readonly ILogger<NotificationsMenuItemViewModel> logger
        = ServiceContainer.Scoped.GetRequiredService<ILogger<NotificationsMenuItemViewModel>>();

    public NotificationsMenuItemViewModel(HomeViewModel parent)
        : base(parent, HomePages.Notifications, "\uE910", typeof(NotificationsPage))
    {
        this.notificationUpdateTimer = new DispatcherTimer() { Interval = TimeSpan.FromMinutes(1) };
        this.notificationUpdateTimer.Tick += OnNotificationTimerTick;
    }

    public override async Task LoadAsync()
    {
        await UpdateNotificationsAsync()
            .ConfigureAwait(false);

        this.syncContext.Post(() => this.notificationUpdateTimer.Start());
    }

    private async void OnNotificationTimerTick(object sender, object e)
    {
        try
        {
            await UpdateNotificationsAsync()
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update notifications");
        }
    }

    private async Task UpdateNotificationsAsync()
    {
        var protocol = protocolService.Protocol;

        var notifications = (await protocol.GetUnreadCountAsync()
            .ConfigureAwait(false))
            .HandleResult();

        NotificationCount = (int)notifications.Count;
        badgeService.UpdateBadge(NotificationCount);
    }
}
