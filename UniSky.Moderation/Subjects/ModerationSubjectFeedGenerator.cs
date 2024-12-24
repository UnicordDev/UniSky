using System.Collections.Generic;
using FishyFlip.Lexicon.App.Bsky.Actor;
using FishyFlip.Lexicon.App.Bsky.Feed;
using FishyFlip.Lexicon.Com.Atproto.Label;

namespace UniSky.Moderation;

public class ModerationSubjectFeedGenerator : ModerationSubject
{
    public ProfileView Creator { get; }

    public ModerationSubjectFeedGenerator(GeneratorView view) : base(view, view.Labels ?? [])
    {
        Creator = view.Creator!;
    }

    public static implicit operator ModerationSubjectFeedGenerator(GeneratorView view)
        => new ModerationSubjectFeedGenerator(view);
}
