using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FishyFlip;
using FishyFlip.Models;
using Microsoft.Extensions.Logging;
using UniSky.Helpers;
using UniSky.Models;
using UniSky.Moderation;
using Windows.ApplicationModel.Resources;
using Windows.Storage;

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
            var did = protocol.Session.Did;

            try
            {
                var moderationCache = await ApplicationData.Current.LocalFolder.GetFileAsync($"ModerationCache.{GetSanitisedDid(did)}.json");
                using (var stream = await moderationCache.OpenStreamForReadAsync())
                {
                    var cache = await JsonSerializer.DeserializeAsync(stream, JsonContext.Default.ModerationCache);
                    if (cache == null)
                        throw new InvalidOperationException("Invalid cache!");
                    if ((DateTimeOffset.Now - cache.SavedAt) > TimeSpan.FromDays(1))
                        throw new InvalidOperationException("Cache expired!");

                    await protocol.ConfigureLabelersAsync(cache.Options.Prefs.Labelers)
                        .ConfigureAwait(false);

                    logger.LogDebug("Configured labelers header on protocol: {Header}", string.Join(", ", cache.Options.Prefs.Labelers.Select(l => l.Id)));

                    ModerationOptions = cache.Options;
                }

                _ = Task.Run(() => FetchModerationOptionsAsync(protocol));
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load moderation cache, falling back!");
            }

            await FetchModerationOptionsAsync(protocol)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // TODO: do i just kill the app here? cause this is _bad_
            logger.LogCritical(ex, "Failed to configure moderation, this is bad.");
        }
    }

    private async Task FetchModerationOptionsAsync(ATProtocol protocol)
    {
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

        ModerationOptions = new ModerationOptions(protocol.Session.Did, moderationPrefs, labelDefs.Labelers, labelDefs.LabelDefs);

        //_ = Task.Run();
        await SaveCacheAsync();
    }

    private async Task SaveCacheAsync()
    {
        try
        {
            var protocol = protocolService.Protocol;
            var did = protocol.Session.Did;
            var cache = new ModerationCache(DateTimeOffset.Now, ModerationOptions);

            var moderationCache = await ApplicationData.Current.LocalFolder.CreateFileAsync($"ModerationCache.{GetSanitisedDid(did)}.json", CreationCollisionOption.ReplaceExisting);

            using var stream = await moderationCache.OpenStreamForWriteAsync();
            await JsonSerializer.SerializeAsync(stream, cache, JsonContext.Default.ModerationCache);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to writer moderation cache");
        }
    }

    public bool TryGetDisplayNameForLabeler(InterpretedLabelValueDefinition labelDef, out string displayName)
    {
        if (labelDef.DefinedBy == null)
        {
            displayName = "Bluesky Moderation Service";
            return true;
        }

        if (ModerationOptions.Labelers.TryGetValue(labelDef.DefinedBy.ToString(), out var detailed))
        {
            displayName = detailed.Creator?.DisplayName ?? "Unknown Labeler";
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

    private static string GetSanitisedDid(ATDid did)
    {
        var sanitisedDid = did.ToString();
        foreach (var item in Path.GetInvalidFileNameChars())
            sanitisedDid = sanitisedDid.Replace(item, '_');

        return sanitisedDid;
    }
}
