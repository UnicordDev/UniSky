using FishyFlip.Lexicon.App.Bsky.Graph;
using FishyFlip.Models;

namespace UniSky.Moderation;

public class ModerationCauseSource
{
    public ModerationCauseSourceType Type { get; internal set; }
    public ListViewBasic? List { get; internal set; }
    public ATDid? Labeler { get; internal set; }

    public override string ToString()
    {
        return $"{{ Type = {Type}, List = {List}, Labeler = {Labeler} }}";
    }
}
