using System;
using System.Collections.Generic;
using System.Text;

namespace UniSky.Moderation.Decisions;

internal static class NotificationDecider
{
    public static ModerationDecision Decide(ModerationSubjectNotification subject, ModerationOptions options)
    {
        var decision = new ModerationDecision(subject.Author.Did!, subject.Author.Did! == options.UserDid, []);
        foreach (var label in subject.Labels)
            decision = decision.AddLabel(LabelTarget.Content, label, options);

        return ModerationDecision.Merge(
            decision,
            AccountDecider.Decide(subject.Author, options),
            ProfileDecider.Decide(subject.Author, options));
    }
}
