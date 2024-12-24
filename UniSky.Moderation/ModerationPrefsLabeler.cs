using System.Collections.Frozen;
using System.Collections.Generic;
using FishyFlip.Lexicon.App.Bsky.Actor;
using FishyFlip.Models;

namespace UniSky.Moderation;

public record ModerationPrefsLabeler(
    ATDid Did,
    IDictionary<string, LabelPreference> Labels,
    bool Redact = false)
{
    public static readonly ModerationPrefsLabeler BSKY_MODERATION_SERVICE
        = new ModerationPrefsLabeler(new ATDid("did:plc:ar7c4by46qjdydhdevvrndac"), new Dictionary<string, LabelPreference>(), true);

    public ModerationPrefsLabeler(LabelerPrefItem prefItem)
        : this(prefItem.Did!, new Dictionary<string, LabelPreference>()) { }
    public ModerationPrefsLabeler(ATDid did, Dictionary<string, LabelPreference> labels)
        : this(did, (IDictionary<string, LabelPreference>)labels) { }

    public string Id
        => Did.Handler + (Redact ? ";redact" : "");
}
