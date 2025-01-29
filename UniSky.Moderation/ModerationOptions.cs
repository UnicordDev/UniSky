using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using FishyFlip;
using FishyFlip.Lexicon;
using FishyFlip.Lexicon.App.Bsky.Actor;
using FishyFlip.Lexicon.App.Bsky.Labeler;
using FishyFlip.Models;
using FishyFlip.Tools;

namespace UniSky.Moderation;

[method: JsonConstructor]
public record ModerationOptions(
    ATDid UserDid,
    ModerationPrefs Prefs,
    IReadOnlyDictionary<string, LabelerViewDetailed> Labelers,
    IReadOnlyDictionary<string, InterpretedLabelValueDefinition[]> LabelDefs)
{

    public ModerationOptions(ATDid userDid, ModerationPrefs prefs, Dictionary<string, LabelerViewDetailed> labelers, Dictionary<string, InterpretedLabelValueDefinition[]> labelDefs)
        : this(userDid, prefs, labelers.ToFrozenDictionary(), labelDefs.ToFrozenDictionary())
    {

    }
}
