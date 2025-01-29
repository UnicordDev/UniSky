using System.Linq;

namespace UniSky.Moderation.Decisions;

internal static class ProfileDecider
{
    public static ModerationDecision Decide(ModerationSubjectProfile subject, ModerationOptions options)
    {
        var decision = new ModerationDecision(subject.Did, subject.Did == options.UserDid, []);
        foreach (var label in subject.Labels.Where(l => l.Uri != null && l.Uri.EndsWith("/app.bsky.actor.profile/self")))
        {
            decision = decision.AddLabel(LabelTarget.Profile, label, options);
        }

        return decision;
    }
}
