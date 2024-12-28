using System.Collections.Generic;
using FishyFlip.Lexicon.App.Bsky.Actor;
using FishyFlip.Lexicon.Com.Atproto.Label;
using FishyFlip.Models;

namespace UniSky.Moderation;

public class ModerationSubjectProfile : ModerationSubject
{
    public ATDid Did { get; }
    public ViewerState? Viewer { get; }

    public ModerationSubjectProfile(ProfileView view) : base(view, view.Labels ?? [])
    {
        Did = view.Did!;
        Viewer = view.Viewer;
    }

    public ModerationSubjectProfile(ProfileViewBasic view) : base(view, view.Labels ?? [])
    {
        Did = view.Did!;
        Viewer = view.Viewer;
    }

    public ModerationSubjectProfile(ProfileViewDetailed view) : base(view, view.Labels ?? [])
    {
        Did = view.Did!;
        Viewer = view.Viewer;
    }

    public static implicit operator ModerationSubjectProfile(ProfileView view)
        => new ModerationSubjectProfile(view);
    public static implicit operator ModerationSubjectProfile(ProfileViewDetailed view)
        => new ModerationSubjectProfile(view);
    public static implicit operator ModerationSubjectProfile(ProfileViewBasic view)
        => new ModerationSubjectProfile(view);
}
