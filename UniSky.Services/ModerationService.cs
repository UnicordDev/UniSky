using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FishyFlip;
using FishyFlip.Lexicon.App.Bsky.Labeler;
using Microsoft.Extensions.Logging;
using UniSky.Moderation;
using Windows.ApplicationModel.Resources;

namespace UniSky.Services;

public record struct LabelStrings(string Name, string Description);

public class ModerationService(
    IProtocolService protocolService,
    ILogger<ModerationService> logger) : IModerationService
{
    private readonly ResourceLoader resources
        = ResourceLoader.GetForViewIndependentUse();

    public ModerationOptions ModerationOptions { get; set; }

    public async Task ConfigureModerationAsync()
    {
        logger.LogInformation("Configuring moderation...");
        try
        {
            var protocol = protocolService.Protocol;
            var moderationPrefs = await protocol.GetModerationPrefsAsync()
                .ConfigureAwait(false);

            logger.LogDebug("Got moderation preferences, AdultContent = {AdultContentEnabled}, Labels = {Labels}, Labelers = {Labelers}, MutedWords = {MutedWords}, HiddenPosts = {HiddenPosts}",
                moderationPrefs.AdultContentEnabled,
                moderationPrefs.Labels.Count,
                moderationPrefs.Labelers.Count,
                moderationPrefs.MutedWords.Count,
                moderationPrefs.HiddenPosts.Count);

            moderationPrefs = moderationPrefs with
            {
                MutedWords =
                [
                    .. moderationPrefs.MutedWords,
                ],
                HiddenPosts =
                [
                    .. moderationPrefs.HiddenPosts,
                ],
            };

            var labelDefs = await protocol.GetLabelDefinitionsAsync(moderationPrefs)
                .ConfigureAwait(false);

            // check if we got all the labelers
            Debug.Assert(labelDefs.Labelers.Count == moderationPrefs.Labelers.Count);
            logger.LogDebug("Fetched label definitions, Expected {LabelerCount}, got {FetchedLabelerCount}",
                moderationPrefs.Labelers.Count,
                labelDefs.Labelers.Count);

            await protocol.ConfigureLabelersAsync(moderationPrefs.Labelers)
                .ConfigureAwait(false);

            logger.LogDebug("Configured labelers header on protocol: {Header}", string.Join(", ", moderationPrefs.Labelers.Select(l => l.Id)));

            ModerationOptions = new ModerationOptions(protocol.Session.Did, moderationPrefs, labelDefs.LabelDefs);
        }
        catch (Exception ex)
        {
            // TODO: do i just kill the app here? cause this is _bad_
            logger.LogCritical(ex, "Failed to configure moderation, this is bad.");
        }
    }


    public bool TryGetDisplayNameForLabeler(InterpretedLabelValueDefinition labelDef, out string displayName)
    {
        if (labelDef.DefinedBy == null || labelDef.Detailed == null)
        {
            displayName = "Bluesky Moderation Service";
            return true;
        }

        if (labelDef.Detailed != null)
        {
            displayName = labelDef.Detailed.Creator?.DisplayName ?? "Unknown Labeler";
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
