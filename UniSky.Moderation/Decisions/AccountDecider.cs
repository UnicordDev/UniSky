using System.Linq;

namespace UniSky.Moderation.Decisions;

internal static class AccountDecider
{
    public static ModerationDecision Decide(ModerationSubjectProfile subject, ModerationOptions options)
    {
        var decision = new ModerationDecision(subject.Did, subject.Did.ToString() == options.UserDid.ToString(), []);
        if (subject.Viewer?.Muted == true)
        {
            if (subject.Viewer?.MutedByList != null)            
                decision = decision.AddMuted(subject.Viewer?.MutedByList);            
            else            
                decision = decision.AddMuted(subject.Viewer?.Muted);            
        }

        if (subject.Viewer?.Blocking != null)
        {
            if (subject.Viewer?.BlockingByList != null)            
                decision = decision.AddBlocking(subject.Viewer?.BlockingByList);            
            else            
                decision = decision.AddBlocking(subject.Viewer?.Blocking);            
        }

        decision = decision.AddBlockedBy(subject.Viewer?.BlockedBy);

        foreach (var label in subject.Labels.Where(l => l.Uri?.EndsWith("/app.bsky.actor.profile/self") != true || l.Val == "!no-unauthenticated"))
            decision = decision.AddLabel(LabelTarget.Account, label, options);

        return decision;
    }
}
