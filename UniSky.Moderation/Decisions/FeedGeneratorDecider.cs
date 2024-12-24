using System;
using System.Collections.Generic;
using System.Text;

namespace UniSky.Moderation.Decisions;

internal static class FeedGeneratorDecider
{
    public static ModerationDecision Decide(ModerationSubjectFeedGenerator subject, ModerationOptions options)
    {
        var decision = new ModerationDecision(subject.Creator.Did!, subject.Creator.Did?.Handler == options.UserDid.Handler, []);
        foreach (var label in subject.Labels)
            decision = decision.AddLabel(LabelTarget.Content, label, options);

        return ModerationDecision.Merge(
            decision,
            AccountDecider.Decide(subject.Creator, options),
            ProfileDecider.Decide(subject.Creator, options));
    }
}
