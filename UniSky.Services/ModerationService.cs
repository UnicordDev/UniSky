using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FishyFlip;
using FishyFlip.Lexicon.App.Bsky.Labeler;
using UniSky.Moderation;
using Windows.ApplicationModel.Resources;

namespace UniSky.Services;

public record struct LabelStrings(string Name, string Description);

public class ModerationService(IProtocolService protocolService) : IModerationService
{
    private readonly ResourceLoader resources
        = ResourceLoader.GetForCurrentView();
    private IReadOnlyDictionary<string, LabelerViewDetailed> labelers;

    public ModerationOptions ModerationOptions { get; set; }

    public async Task ConfigureModerationAsync()
    {
        var protocol = protocolService.Protocol;
        var moderationPrefs = await protocol.GetModerationPrefsAsync()
            .ConfigureAwait(false);

        var labelDefs = await protocol.GetLabelDefinitionsAsync(moderationPrefs)
            .ConfigureAwait(false);

        await protocol.ConfigureLabelersAsync(moderationPrefs.Labelers)
            .ConfigureAwait(false);

        labelers = labelDefs.Labelers;
        ModerationOptions = new ModerationOptions(protocol.Session.Did, moderationPrefs, labelDefs.LabelDefs);
    }

    public bool TryGetDisplayNameForLabeler(InterpretedLabelValueDefinition labelDef, out string displayName)
    {
        if (labelDef.DefinedBy == null)
        {
            displayName = "Bluesky Moderation Service";
            return true;
        }

        if (labelers.TryGetValue(labelDef.DefinedBy.ToString(), out var labelerViewDetailed))
        {
            displayName = labelerViewDetailed.Creator?.DisplayName ?? "Unknown Labeler";
            return true;
        }

        displayName = null;
        return false;
    }

    public bool TryGetLocalisedStringsForLabel(InterpretedLabelValueDefinition labelDef, out LabelStrings label)
    {
        label = default;

        if (labelDef.DefinedBy == null)
        {
            // sanitise this for resource lookup
            var sanitisedIdentifier = labelDef.Identifier
                .ToUpperInvariant()
                .Replace('-', '_')
                .TrimStart('!');

            var nameResId = $"GlobalLabel_{sanitisedIdentifier}_Name";
            var descriptionResId = $"GlobalLabel_{sanitisedIdentifier}_Description";

            label = new LabelStrings(resources.GetString(nameResId), resources.GetString(descriptionResId));
            return true;
        }

        var locale = labelDef.Locales.FirstOrDefault();
        if (locale == null)
            return false;

        label = new LabelStrings(locale.Name, locale.Description);
        return true;
    }
}
