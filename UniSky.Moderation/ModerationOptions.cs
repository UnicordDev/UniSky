using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FishyFlip;
using FishyFlip.Lexicon;
using FishyFlip.Lexicon.App.Bsky.Actor;
using FishyFlip.Models;
using FishyFlip.Tools;

namespace UniSky.Moderation;

public record ModerationOptions(
    ATDid UserDid,
    ModerationPrefs Prefs,
    IReadOnlyDictionary<string, InterpretedLabelValueDefinition[]> LabelDefs)
{
    public ModerationOptions(ATDid userDid, ModerationPrefs prefs, Dictionary<string, InterpretedLabelValueDefinition[]> labelDefs)
        : this(userDid, prefs, labelDefs.ToFrozenDictionary())
    {

    }
}
