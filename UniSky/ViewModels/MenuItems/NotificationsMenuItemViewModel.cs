using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UniSky.Extensions;
using UniSky.Messages;
using UniSky.Pages;
using UniSky.Services;
using Windows.ApplicationModel;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace UniSky.ViewModels;

public partial class NotificationsMenuItemViewModel : MenuItemViewModel
{
    /// <summary>Matches social-app's UPDATE_INTERVAL.</summary>
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    private readonly DispatcherTimer notificationUpdateTimer;

    private readonly IBadgeService badgeService
        = ServiceContainer.Scoped.GetRequiredService<IBadgeService>();
    private readonly INotificationFeedService feedService
        = ServiceContainer.Scoped.GetRequiredService<INotificationFeedService>();
    private readonly ILogger<NotificationsMenuItemViewModel> logger
        = ServiceContainer.Scoped.GetRequiredService<ILogger<NotificationsMenuItemViewModel>>();

    private int inFlight;
    private bool started;

    public NotificationsMenuItemViewModel(HomeViewModel parent)
        : base(parent, HomePages.Notifications, "\uE910", typeof(NotificationsPage))
    {
        this.notificationUpdateTimer = new DispatcherTimer() { Interval = PollInterval };
        this.notificationUpdateTimer.Tick += OnNotificationTimerTick;

        WeakReferenceMessenger.Default.Register<MarkAsReadNotification>(this,
            (o, e) => ((NotificationsMenuItemViewModel)o).OnMarkedAsRead(e));
            
        Window.Current.CoreWindow.VisibilityChanged += OnVisibilityChanged;
        Application.Current.Suspending += OnSuspending;
        Application.Current.Resuming += OnResuming;
    }

    private void OnMarkedAsRead(MarkAsReadNotification e)
    {
        NotificationCount = 0;
        badgeService.UpdateBadge(0);
    }

    public override async Task LoadAsync()
    {
        await UpdateNotificationsAsync()
            .ConfigureAwait(false);

        this.syncContext.Post(() =>
        {
            started = true;
            this.notificationUpdateTimer.Start();
        });
    }

    private void OnVisibilityChanged(CoreWindow sender, VisibilityChangedEventArgs args)
    {
        if (args.Visible)
            Resume();
        else
            notificationUpdateTimer.Stop();
    }

    private void OnSuspending(object sender, SuspendingEventArgs e)
        => notificationUpdateTimer.Stop();

    private void OnResuming(object sender, object e)
        => syncContext.Post(Resume);

    private void Resume()
    {
        if (!started)
            return;

        notificationUpdateTimer.Start();
        _ = PollAsync();
    }

    private void OnNotificationTimerTick(object sender, object e)
        => _ = PollAsync();

    private async Task PollAsync()
    {
        try
        {
            await UpdateNotificationsAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update notifications");
        }
    }

    private async Task UpdateNotificationsAsync()
    {
        if (Interlocked.CompareExchange(ref inFlight, 1, 0) != 0)
            return;

        try
        {
            var count = await feedService.GetUnreadCountAsync(null)
                .ConfigureAwait(false);

            NotificationCount = count;
            badgeService.UpdateBadge(count);
        }
        finally
        {
            Volatile.Write(ref inFlight, 0);
        }
    }
}
