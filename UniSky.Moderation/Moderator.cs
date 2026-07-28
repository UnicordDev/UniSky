using System.Collections.Generic;
using FishyFlip.Lexicon.App.Bsky.Richtext;
using UniSky.Moderation.Decisions;

namespace UniSky.Moderation;

public readonly struct Moderator(ModerationOptions options)
{
    private readonly ModerationOptions options = options;

    public bool HasMutedWord(string text,
                             IReadOnlyList<Facet>? facets = null,
                             IReadOnlyList<string>? outlineTags = null,
                             IReadOnlyList<string>? languages = null,
                             bool actorIsFollowed = false)
    {
        var mutedWords = options.Prefs?.MutedWords;
        if (mutedWords is null || mutedWords.Count == 0)
            return false;

        return PostDecider.HasMutedWord(mutedWords, text, facets ?? [], outlineTags ?? [], languages ?? [], actorIsFollowed);
    }

    public ModerationDecision ModerateProfile(ModerationSubjectProfile profile) 
        => ModerationDecision.Merge(
            AccountDecider.Decide(profile, options),
            ProfileDecider.Decide(profile, options));

    public ModerationDecision ModeratePost(ModerationSubjectPost post)
        => PostDecider.Decide(post, options);

    public ModerationDecision ModerateNotification(ModerationSubjectNotification notification)
        => NotificationDecider.Decide(notification, options);

    public ModerationDecision ModerateFeedGenerator(ModerationSubjectFeedGenerator generator)
        => FeedGeneratorDecider.Decide(generator, options);

    public ModerationDecision ModerateUserList(ModerationSubjectUserList userList)
        => UserListDecider.Decide(userList, options);
}
