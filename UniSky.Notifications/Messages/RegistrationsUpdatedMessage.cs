using CommunityToolkit.Mvvm.Messaging.Messages;

namespace UniSky.Notifications.Messages;

public class RegistrationsUpdatedMessage : CollectionRequestMessage<Task>
{
    public string? Did { get; }

    public RegistrationsUpdatedMessage()
    {
    }

    public RegistrationsUpdatedMessage(string did)
    {
        Did = did;
    }
}
