using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FishyFlip.Lexicon.App.Bsky.Graph;
using FishyFlip.Lexicon.Com.Atproto.Label;
using FishyFlip.Models;

namespace UniSky.Moderation;

public class ModerationDecision(ATDid did, bool isMe, IEnumerable<ModerationCause> causes)
{
    private static readonly Regex CustomLabelValueRegex = new Regex("^[a-z-]+$", RegexOptions.ECMAScript | RegexOptions.Compiled);

    private readonly List<ModerationCause> causes = [.. causes];

    public ATDid Did { get; } = did;
    public bool IsMe { get; } = isMe;
    public IReadOnlyList<ModerationCause> Causes
        => causes;

    public ModerationCause? BlockCause
        => Causes.FirstOrDefault(c => c.Type is ModerationCauseType.Blocking or ModerationCauseType.BlockedBy or ModerationCauseType.BlockOther);
    public ModerationCause? MuteCause
        => Causes.FirstOrDefault(c => c.Type is ModerationCauseType.Muted);
    public IEnumerable<ModerationCause> LabelCauses
        => Causes.Where(c => c.Type is ModerationCauseType.Label);

    public bool Blocked
        => BlockCause != null;
    public bool Muted
        => MuteCause != null;

    public static ModerationDecision Merge(params ModerationDecision?[] decisions)
    {
        var dec = decisions.Where(d => d != null);

        return new ModerationDecision(dec.FirstOrDefault()!.Did, dec.FirstOrDefault()!.IsMe, dec.SelectMany(s => s!.Causes));
    }

    public ModerationDecision Downgrade()
    {
        foreach (var item in Causes)
            item.Downgraded = true;

        return this;
    }

    public ModerationUI GetUI(ModerationContext context)
    {
        var filters = new List<ModerationCause>();
        var blurs = new List<ModerationCause>();
        var alerts = new List<ModerationCause>();
        var informs = new List<ModerationCause>();
        var noOverride = false;

        void HandleStandardCause(ModerationContext context,
                            ModerationCause cause,
                            ReadOnlySpan<ModerationContext> contexts,
                            ModerationBehavior behavior)
        {
            for (var i = 0; i < contexts.Length; i++)
            {
                if (contexts[i] == context)
                {
                    filters.Add(cause);
                    break;
                }
            }

            if (!cause.Downgraded)
            {
                switch (behavior[context])
                {
                    case ModerationBehaviorType.Blur:
                        blurs.Add(cause);
                        break;
                    case ModerationBehaviorType.Alert:
                        alerts.Add(cause);
                        break;
                    case ModerationBehaviorType.Inform:
                        informs.Add(cause);
                        break;
                }
            }
        }

        foreach (var cause in this.Causes)
        {
            if (cause.Type is ModerationCauseType.Blocking or ModerationCauseType.BlockedBy or ModerationCauseType.BlockOther)
            {
                if (this.IsMe) continue;

                HandleStandardCause(
                    context,
                    cause,
                    [ModerationContext.ProfileList, ModerationContext.ContentList],
                    ModerationBehavior.BlockBehaviour);

                if (!cause.Downgraded && ModerationBehavior.BlockBehaviour[context] == ModerationBehaviorType.Blur)
                    noOverride = true;
            }
            else if (cause.Type is ModerationCauseType.Muted)
            {
                if (this.IsMe) continue;

                HandleStandardCause(
                    context,
                    cause,
                    [ModerationContext.ProfileList, ModerationContext.ContentList],
                    ModerationBehavior.MuteBehaviour);
            }
            else if (cause.Type is ModerationCauseType.MuteWord)
            {
                if (this.IsMe) continue;

                HandleStandardCause(
                    context,
                    cause,
                    [ModerationContext.ContentList],
                    ModerationBehavior.MuteWordBehavour);
            }
            else if (cause.Type is ModerationCauseType.Hidden)
            {
                HandleStandardCause(
                    context,
                    cause,
                    [ModerationContext.ProfileList, ModerationContext.ContentList],
                    ModerationBehavior.HideBehaviour);
            }
            else if (cause.Type is ModerationCauseType.Label)
            {
                if (cause is not LabelModerationCause labelCause)
                    throw new InvalidOperationException();

                if (context is ModerationContext.ProfileList && labelCause.Target == LabelTarget.Account)
                {
                    if (labelCause.Setting == LabelPreference.Hide && !IsMe)
                        filters.Add(cause);
                }
                else if (context is ModerationContext.ContentList && (labelCause.Target is (LabelTarget.Account or LabelTarget.Content)))
                {
                    if (labelCause.Setting == LabelPreference.Hide && !IsMe)
                        filters.Add(cause);
                }

                if (!cause.Downgraded)
                {
                    switch (labelCause.Behavior[context])
                    {
                        case ModerationBehaviorType.Blur:
                            blurs.Add(cause);
                            if (labelCause.NoOverride && !IsMe)
                                noOverride = true;
                            break;
                        case ModerationBehaviorType.Alert:
                            alerts.Add(cause);
                            break;
                        case ModerationBehaviorType.Inform:
                            informs.Add(cause);
                            break;
                    }
                }
            }
        }

        return new ModerationUI(noOverride, filters, blurs, alerts, informs);
    }

    public ModerationDecision AddHidden(bool? hidden)
    {
        if (hidden == true)
        {
            this.causes.Add(new ModerationCause()
            {
                Type = ModerationCauseType.Hidden,
                Source = new() { Type = ModerationCauseSourceType.User },
                Priority = 6
            });
        }

        return this;
    }

    public ModerationDecision AddMutedWord(bool? mutedWord)
    {
        if (mutedWord == true)
        {
            this.causes.Add(new ModerationCause()
            {
                Type = ModerationCauseType.MuteWord,
                Source = new() { Type = ModerationCauseSourceType.User },
                Priority = 6
            });
        }

        return this;
    }

    public ModerationDecision AddBlocking(ATUri? blocking)
    {
        if (blocking != null)
        {
            this.causes.Add(new ModerationCause()
            {
                Type = ModerationCauseType.Blocking,
                Source = new() { Type = ModerationCauseSourceType.User },
                Priority = 3
            });
        }

        return this;
    }

    public ModerationDecision AddBlocking(ListViewBasic? blockingByList)
    {
        if (blockingByList != null)
        {
            this.causes.Add(new ModerationCause()
            {
                Type = ModerationCauseType.Blocking,
                Source = new() { Type = ModerationCauseSourceType.List, List = blockingByList },
                Priority = 3
            });
        }

        return this;
    }

    public ModerationDecision AddBlockedBy(bool? blockedBy)
    {
        if (blockedBy == true)
        {
            this.causes.Add(new ModerationCause()
            {
                Type = ModerationCauseType.BlockedBy,
                Source = new() { Type = ModerationCauseSourceType.User },
                Priority = 4
            });
        }

        return this;
    }

    public ModerationDecision AddBlockOther(bool? blockOther)
    {
        if (blockOther == true)
        {
            this.causes.Add(new ModerationCause()
            {
                Type = ModerationCauseType.BlockOther,
                Source = new() { Type = ModerationCauseSourceType.User },
                Priority = 4
            });
        }

        return this;
    }

    public ModerationDecision AddMuted(bool? muted)
    {
        if (muted == true)
        {
            this.causes.Add(new ModerationCause()
            {
                Type = ModerationCauseType.Muted,
                Source = new() { Type = ModerationCauseSourceType.User },
                Priority = 6
            });
        }

        return this;
    }

    public ModerationDecision AddMuted(ListViewBasic? mutedByList)
    {
        if (mutedByList != null)
        {
            this.causes.Add(new ModerationCause()
            {
                Type = ModerationCauseType.Muted,
                Source = new() { Type = ModerationCauseSourceType.List, List = mutedByList },
                Priority = 6
            });
        }

        return this;
    }

    public ModerationDecision AddLabel(LabelTarget target, Label label, ModerationOptions options)
    {
        InterpretedLabelValueDefinition labelDef = null!;
        if (CustomLabelValueRegex.IsMatch(label.Val))
        {
            if (options.LabelDefs?.TryGetValue(label.Src?.ToString() ?? "", out var values) == true)
                labelDef = values.FirstOrDefault(v => v.Identifier == label.Val);
        }

        if (labelDef == null)
        {
            if (!KnownLabelsHelper.Labels.TryGetValue(KnownLabelsHelper.FromString(label.Val!), out labelDef))
                return this;
        }

        var isSelf = label.Src!.ToString() == this.Did.ToString();
        var labeler = isSelf ? null : options.Prefs.Labelers.FirstOrDefault(s => s.Did.ToString() == label.Src!.ToString());
        if (!isSelf && labeler == null)
            return this;

        if (isSelf && labelDef.Flags.HasFlag(LabelValueDefinitionFlags.NoSelf))
            return this;

        var labelPref = labelDef.DefaultSetting ?? LabelPreference.Ignore;
        if (!labelDef.Configurable)
            labelPref = labelDef.DefaultSetting ?? LabelPreference.Hide;
        else if (labelDef.Flags.HasFlag(LabelValueDefinitionFlags.Adult) && !options.Prefs.AdultContentEnabled)
            labelPref = LabelPreference.Hide;
        else if (labeler?.Labels.TryGetValue(labelDef.Identifier, out var pref) == true)
            labelPref = pref;
        else if (options.Prefs.Labels.TryGetValue(labelDef.Identifier, out pref) == true)
            labelPref = pref;

        if (labelPref == LabelPreference.Ignore)
            return this;

        if (labelDef.Flags.HasFlag(LabelValueDefinitionFlags.UnAuthed) && options.UserDid != null)
            return this;

        var priority = 0;
        var behavour = labelDef.Behaviors[target];
        var severity = MeasureModerationBehaviorSeverity(behavour);
        if (labelDef.Flags.HasFlag(LabelValueDefinitionFlags.NoOverride) ||
            (labelDef.Flags.HasFlag(LabelValueDefinitionFlags.Adult) && !options.Prefs.AdultContentEnabled))
        {
            priority = 1;
        }
        else if (labelPref == LabelPreference.Hide)
        {
            priority = 2;
        }
        else if (severity == ModerationBehaviorSeverity.High)
        {
            priority = 5;
        }
        else if (severity == ModerationBehaviorSeverity.Medium)
        {
            priority = 7;
        }
        else
        {
            priority = 8;
        }

        var noOverride = labelDef.Flags.HasFlag(LabelValueDefinitionFlags.NoOverride) ||
            (labelDef.Flags.HasFlag(LabelValueDefinitionFlags.Adult) && !options.Prefs.AdultContentEnabled);

        this.causes.Add(new LabelModerationCause()
        {
            Source = isSelf || labeler == null
                ? new ModerationCauseSource() { Type = ModerationCauseSourceType.User }
                : new ModerationCauseSource() { Type = ModerationCauseSourceType.Labeler, Labeler = labeler.Did },
            Label = label,
            LabelDef = labelDef,
            Target = target,
            Setting = labelPref,
            Behavior = labelDef.Behaviors[target],
            NoOverride = noOverride,
            Priority = (byte)priority
        });

        return this;
    }


    enum ModerationBehaviorSeverity
    {
        Low, Medium, High
    };

    private ModerationBehaviorSeverity MeasureModerationBehaviorSeverity(ModerationBehavior? beh)
    {
        if (beh == null) return ModerationBehaviorSeverity.Low;
        return beh.Value switch
        {
            { ProfileView: ModerationBehaviorType.Blur, ContentView: ModerationBehaviorType.Blur } => ModerationBehaviorSeverity.High,
            { ContentList: ModerationBehaviorType.Blur, ContentMedia: ModerationBehaviorType.Blur } => ModerationBehaviorSeverity.Medium,
            _ => ModerationBehaviorSeverity.Low
        };
    }
}