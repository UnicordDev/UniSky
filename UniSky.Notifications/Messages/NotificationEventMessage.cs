using CommunityToolkit.Mvvm.Messaging.Messages;
using UniSky.Notifications.Models;

namespace UniSky.Notifications.Messages;

public class NotificationEventMessage : CollectionRequestMessage<Task>
{
    public NotificationEventMessage(NotificationEvent @event)
    {
        Event = @event;
    }

    public NotificationEvent Event { get; }
}
