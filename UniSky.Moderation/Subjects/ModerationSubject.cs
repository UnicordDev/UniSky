using System.Collections.Generic;
using FishyFlip.Lexicon;
using FishyFlip.Lexicon.Com.Atproto.Label;

namespace UniSky.Moderation;

public abstract class ModerationSubject(ATObject source, IReadOnlyList<Label> labels)
{
    public ATObject Source { get; } = source;
    public IReadOnlyList<Label> Labels { get; } = labels;
}
