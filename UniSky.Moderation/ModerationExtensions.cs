using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FishyFlip;
using FishyFlip.Lexicon.App.Bsky.Actor;
using FishyFlip.Lexicon.App.Bsky.Labeler;
using FishyFlip.Models;
using FishyFlip.Tools;

namespace UniSky.Moderation;

public static class ModerationExtensions
{
    public static Task ConfigureLabelersAsync(this ATProtocol protocol, IReadOnlyList<ModerationPrefsLabeler> labelers)
    {
        foreach (var items in labelers)
        {
            protocol.Options.LabelParameters.Add(LabelParameter.Create(items.Did.Handler, items.Redact ? ["redact"] : []));
        }

        return Task.CompletedTask;
    }

    public static async Task<ModerationPrefs> GetModerationPrefsAsync(this ATProtocol protocol)
    {
        var adultContent = false;
        var currentDid = protocol.Session?.Did
            ?? throw new InvalidOperationException("A session must be initialized before creating ModerationOptions");

        var preferences = (await protocol.GetPreferencesAsync()
              .ConfigureAwait(false))
              .HandleResult();

        var labelPrefs = new List<ContentLabelPref>();
        var labelers = new List<ModerationPrefsLabeler>() { ModerationPrefsLabeler.BSKY_MODERATION_SERVICE };
        var mutedWords = new List<MutedWord>();
        var hiddenPosts = new List<ATUri>();
        var labels = new Dictionary<string, LabelPreference>();
        foreach (var pref in preferences!.Preferences ?? [])
        {
            switch (pref)
            {
                case AdultContentPref acp:
                    adultContent = acp.Enabled == true;
                    break;
                case ContentLabelPref clp:
                    if (clp.Visibility == "show")
                        clp.Visibility = "ignore";

                    labelPrefs.Add(clp);
                    break;
                case LabelersPref lp:
                    foreach (var l in lp.Labelers ?? [])
                        labelers.Add(new ModerationPrefsLabeler(l));
                    break;
                case MutedWordsPref mwp:
                    foreach (var w in mwp.Items ?? [])
                    {
                        w.ActorTarget ??= "all";
                        mutedWords.Add(w);
                    }
                    break;
                case HiddenPostsPref hpp:
                    foreach (var h in hpp.Items ?? [])
                        hiddenPosts.Add(h);
                    break;
            }
        }

        foreach (var pref in labelPrefs)
        {
            if (pref.LabelerDid != null)
            {
                var labeler = labelers.FirstOrDefault(l => l.Did.Handler == pref.LabelerDid.Handler);
                if (labeler == null) continue;

                labeler.Labels[pref.Label!] = pref.Visibility switch
                {
                    "hide" => LabelPreference.Hide,
                    "warn" => LabelPreference.Warn,
                    _ => LabelPreference.Ignore
                };
            }
            else
            {
                labels[pref.Label!] = pref.Visibility switch
                {
                    "hide" => LabelPreference.Hide,
                    "warn" => LabelPreference.Warn,
                    _ => LabelPreference.Ignore
                };
            }
        }

        return new ModerationPrefs(adultContent, labels, labelers, mutedWords, hiddenPosts);
    }

    public static async Task<LabelDefinitionsResult> GetLabelDefinitionsAsync(
        this ATProtocol protocol,
        ModerationPrefs prefs)
    {
        List<ATDid> dids = [.. prefs.Labelers.Select(l => l.Did).Where(d => d != null)];
        var labelers = (await protocol.GetServicesAsync(dids, detailed: true)
            .ConfigureAwait(false))
            .HandleResult();

        var labelersDict = new Dictionary<string, LabelerViewDetailed>();
        var labelDefs = new Dictionary<string, InterpretedLabelValueDefinition[]>();
        var defs = (labelers?.Views?.OfType<LabelerViewDetailed>()?.ToList()) ?? [];
        foreach (var def in defs)
        {
            if (def.Creator?.Did == null)
                continue;

            labelersDict[def.Creator.Did.Handler] = def;
            labelDefs[def.Creator.Did.Handler] =
                def.Policies?.LabelValueDefinitions?.Select(s => new InterpretedLabelValueDefinition(s, def.Creator.Did)).ToArray() ?? [];
        }

        return new(labelersDict.ToFrozenDictionary(), labelDefs.ToFrozenDictionary());
    }
}

public record struct LabelDefinitionsResult(
    IReadOnlyDictionary<string, LabelerViewDetailed> Labelers,
    IReadOnlyDictionary<string, InterpretedLabelValueDefinition[]> LabelDefs)
{
}