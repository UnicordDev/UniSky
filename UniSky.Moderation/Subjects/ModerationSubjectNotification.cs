using System.Collections.Generic;
using FishyFlip.Lexicon.App.Bsky.Actor;
using FishyFlip.Lexicon.App.Bsky.Notification;

namespace UniSky.Moderation;

public class ModerationSubjectNotification : ModerationSubject
{
    public ProfileView Author { get; }

    public ModerationSubjectNotification(Notification view) : base(view, view.Labels ?? [])
    {
        Author = view.Author!;
    }

    public static implicit operator ModerationSubjectNotification(Notification view)
        => new ModerationSubjectNotification(view);
}
