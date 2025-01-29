using System;
using System.Collections.Generic;
using System.Text;
using FishyFlip.Lexicon.App.Bsky.Actor;
using FishyFlip.Models;

namespace UniSky.Moderation.Decisions;

internal static class UserListDecider
{
    public static ModerationDecision Decide(ModerationSubjectUserList subject, ModerationOptions options)
    {
        ModerationDecision decision;
        if (subject.Creator is ProfileView { Did: { } } creator)
        {
            decision = new ModerationDecision(creator.Did, creator.Did == options.UserDid, []);
            foreach (var label in subject.Labels)
                decision = decision.AddLabel(LabelTarget.Content, label, options);

            return ModerationDecision.Merge(
                decision,
                AccountDecider.Decide(creator, options),
                ProfileDecider.Decide(creator, options)
            );
        }

        var did = new ATDid(subject.Uri.Hostname);
        decision = new ModerationDecision(did, did == options.UserDid, []);
        foreach (var label in subject.Labels)
            decision = decision.AddLabel(LabelTarget.Content, label, options);

        return decision;
    }
}
