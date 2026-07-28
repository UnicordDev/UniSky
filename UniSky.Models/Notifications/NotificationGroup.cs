using System;
using System.Collections.Generic;
using FishyFlip.Lexicon.App.Bsky.Notification;
using FishyFlip.Models;

namespace UniSky.Models.Notifications;

public sealed class NotificationGroup
{
    private readonly List<Notification> additional = [];

    public NotificationGroup(NotificationKind kind, Notification head)
    {
        Kind = kind;
        Head = head ?? throw new ArgumentNullException(nameof(head));
        SubjectUri = NotificationTypes.GetSubjectUri(kind, head);
    }

    public NotificationKind Kind { get; }
    
    public Notification Head { get; }

    public IReadOnlyList<Notification> Additional => additional;
    
    public ATUri? SubjectUri { get; }
    
    public string? Key => Head.Cid;

    public int Count => additional.Count + 1;

    public DateTime IndexedAt => Head.IndexedAt ?? DateTime.MinValue;

    internal void Add(Notification notification)
        => additional.Add(notification);
        
    public IEnumerable<Notification> All()
    {
        yield return Head;
        for (var i = 0; i < additional.Count; i++)
            yield return additional[i];
    }
}
