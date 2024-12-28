using System.Collections.Generic;
using System.Threading.Tasks;
using FishyFlip.Lexicon.App.Bsky.Labeler;
using UniSky.Moderation;

namespace UniSky.Services;

public interface IModerationService
{
    ModerationOptions ModerationOptions { get; set; }

    Task ConfigureModerationAsync();

    bool TryGetDisplayNameForLabeler(InterpretedLabelValueDefinition labelDef, out string displayName);

    bool TryGetLocalisedStringsForLabel(InterpretedLabelValueDefinition labelDef, out LabelStrings label);
}