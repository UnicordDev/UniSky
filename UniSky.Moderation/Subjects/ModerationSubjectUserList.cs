using FishyFlip.Lexicon.App.Bsky.Actor;
using FishyFlip.Lexicon.App.Bsky.Graph;
using FishyFlip.Models;

namespace UniSky.Moderation;

public class ModerationSubjectUserList : ModerationSubject
{
    public ATUri Uri { get; }
    public ProfileView? Creator { get; }

    public ModerationSubjectUserList(ListView view) : base(view, view.Labels ?? [])
    {
        Uri = view.Uri!;
        Creator = view.Creator;
    }

    public ModerationSubjectUserList(ListViewBasic view) : base(view, view.Labels ?? [])
    {
        Uri = view.Uri!;
        Creator = null;
    }

    public static implicit operator ModerationSubjectUserList(ListView view)
        => new ModerationSubjectUserList(view);

    public static implicit operator ModerationSubjectUserList(ListViewBasic view)
        => new ModerationSubjectUserList(view);
}