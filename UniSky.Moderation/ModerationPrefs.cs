using System.Collections.Frozen;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using FishyFlip.Lexicon.App.Bsky.Actor;
using FishyFlip.Models;

namespace UniSky.Moderation;

[method: JsonConstructor]
public record ModerationPrefs(
    bool AdultContentEnabled,
    IReadOnlyDictionary<string, LabelPreference> Labels,
    IReadOnlyList<ModerationPrefsLabeler> Labelers,
    IReadOnlyList<MutedWord> MutedWords,
    IReadOnlyList<ATUri> HiddenPosts)
{
    public ModerationPrefs(
        bool adultContentEnabled,
        Dictionary<string, LabelPreference> labels,
        IReadOnlyList<ModerationPrefsLabeler> labelers,
        IReadOnlyList<MutedWord> mutedWords,
        IReadOnlyList<ATUri> hiddenPosts)
        : this(adultContentEnabled, labels.ToFrozenDictionary(), labelers, mutedWords, hiddenPosts)
    {

    }
}
