using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FishyFlip.Lexicon;
using FishyFlip.Lexicon.App.Bsky.Actor;
using FishyFlip.Lexicon.App.Bsky.Embed;
using FishyFlip.Lexicon.App.Bsky.Feed;
using FishyFlip.Lexicon.App.Bsky.Richtext;
using FishyFlip.Models;

namespace UniSky.Moderation.Decisions;

internal static class PostDecider
{
    private static readonly Regex Punctuaton = new Regex("\\p{P}+", RegexOptions.ECMAScript | RegexOptions.Compiled);
    private static readonly Regex Whitespace = new Regex("\\s", RegexOptions.ECMAScript | RegexOptions.Compiled);
    private static readonly Regex SpaceOrPunctuation = new Regex("(?:\\s|\\p{P})+?", RegexOptions.ECMAScript | RegexOptions.Compiled);
    private static readonly Regex LeadingTrailingPunctuation = new Regex("(?:^\\p{P}+|\\p{P}+$)", RegexOptions.ECMAScript | RegexOptions.Compiled);
    private static readonly Regex Escape = new Regex("[[\\]{}()*+?.\\\\^$|\\s]", RegexOptions.ECMAScript | RegexOptions.Compiled);
    private static readonly Regex Separators = new Regex("[/\\-–—()[\\]_]+", RegexOptions.ECMAScript | RegexOptions.Compiled);
    private static readonly Regex WordBoundary = new Regex("[\\s\\n\\t\\r\\f\\v]+?", RegexOptions.ECMAScript | RegexOptions.Compiled);

    public static ModerationDecision Decide(ModerationSubjectPost subject, ModerationOptions options)
    {
        var decision = new ModerationDecision(subject.Author!.Did!, subject.Author!.Did!.Handler == options.UserDid.Handler, []);
        foreach (var label in subject.Labels)
            decision = decision.AddLabel(LabelTarget.Content, label, options);

        decision = decision.AddHidden(CheckHiddenPost(subject, options.Prefs.HiddenPosts));
        if (!decision.IsMe)
            decision = decision.AddMutedWord(CheckMutedWords(subject, options.Prefs.MutedWords));

        var embed = DecideEmbed(subject.Embed, options)?.Downgrade();

        return ModerationDecision.Merge(
            decision,
            embed,
            AccountDecider.Decide(subject.Author, options),
            ProfileDecider.Decide(subject.Author, options)
        );
    }

    private static ModerationDecision? DecideEmbed(ATObject? subject, ModerationOptions options)
    {
        if (subject is ViewRecordDef { Record: ViewRecord viewRecord })
            return DecideQuotedPost(viewRecord, options);
        else if (subject is ViewRecordWithMedia { Record.Record: ViewRecord mediaViewRecord })
            return DecideQuotedPost(mediaViewRecord, options);
        else if (subject is ViewRecordDef { Record: ViewBlocked blocked })
            return DecideBlockedQuotedPost(blocked, options);
        else if (subject is ViewRecordWithMedia { Record.Record: ViewBlocked mediaBlocked })
            return DecideBlockedQuotedPost(mediaBlocked, options);

        return null;
    }

    private static ModerationDecision DecideQuotedPost(ViewRecord subject, ModerationOptions options)
    {
        var decision = new ModerationDecision(subject.Author!.Did!, subject.Author!.Did!.Handler == options.UserDid.Handler, []);
        foreach (var label in subject.Labels ?? [])
            decision = decision.AddLabel(LabelTarget.Content, label, options);

        return ModerationDecision.Merge(
            decision,
            AccountDecider.Decide(subject.Author, options),
            ProfileDecider.Decide(subject.Author, options));
    }

    private static ModerationDecision DecideBlockedQuotedPost(ViewBlocked subject, ModerationOptions options)
    {
        var decision = new ModerationDecision(subject.Author!.Did!, subject.Author!.Did!.Handler == options.UserDid.Handler, []);

        if (subject.Author.Viewer?.Muted == true)
        {
            if (subject.Author.Viewer?.MutedByList != null)
                decision = decision.AddMuted(subject.Author.Viewer?.MutedByList);
            else
                decision = decision.AddMuted(subject.Author.Viewer?.Muted);
        }

        if (subject.Author.Viewer?.Blocking != null)
        {
            if (subject.Author.Viewer?.BlockingByList != null)
                decision = decision.AddBlocking(subject.Author.Viewer?.BlockingByList);
            else
                decision = decision.AddBlocking(subject.Author.Viewer?.Blocking);
        }

        decision.AddBlockedBy(subject.Author.Viewer?.BlockedBy);

        return decision;
    }

    private static bool CheckHiddenPost(ModerationSubjectPost subjectPost, IReadOnlyList<ATUri> hiddenPosts)
    {
        if (hiddenPosts == null || hiddenPosts.Count == 0)
            return false;

        if (hiddenPosts.Any(p => p.Href == subjectPost.Uri.Href))
            return true;

        if (subjectPost.Embed is ViewRecordDef { Record: ViewRecord viewRecord } &&
            hiddenPosts.Any(p => p.Href == viewRecord.Uri?.Href))
            return true;

        if (subjectPost.Embed is ViewRecordWithMedia { Record.Record: ViewRecord mediaViewRecord } &&
            hiddenPosts.Any(p => p.Href == mediaViewRecord.Uri?.Href))
            return true;

        return false;
    }

    // this is pattern matching hell please send help
    private static bool? CheckMutedWords(ModerationSubjectPost subject, IReadOnlyList<MutedWord> mutedWords)
    {
        if (mutedWords.Count == 0)
            return false;

        if (subject.Record is Post post)
        {
            if (HasMutedWord(mutedWords, post.Text ?? "", post.Facets ?? [], post.Tags ?? [], post.Langs ?? [], subject.Author))
                return true;

            if (post.Embed is EmbedImages images)
            {
                foreach (var image in images.Images ?? [])
                {
                    if (HasMutedWord(mutedWords, image.Alt ?? "", [], [], post.Langs ?? [], subject.Author))
                        return true;
                }
            }
        }

        if (subject.Embed != null)
        {
            // quote post
            if (subject.Embed is ViewRecordDef { Record: ViewRecord { Value: Post embeddedPost, Author: { } embeddedAuthor } })
            {
                if (HasMutedWord(mutedWords, embeddedPost.Text ?? "", embeddedPost.Facets ?? [], embeddedPost.Tags ?? [], embeddedPost.Langs ?? [], embeddedAuthor))
                    return true;

                if (embeddedPost.Embed is EmbedImages images)
                {
                    foreach (var image in images.Images ?? [])
                    {
                        if (HasMutedWord(mutedWords, image.Alt ?? "", [], [], embeddedPost.Langs ?? [], embeddedAuthor))
                            return true;
                    }
                }

                if (embeddedPost.Embed is EmbedExternal { External: { } external })
                {
                    if (HasMutedWord(mutedWords, $"{external.Title} {external.Description}", [], [], [], embeddedAuthor))
                        return true;
                }

                if (embeddedPost.Embed is RecordWithMedia recordWithMedia)
                {
                    if (recordWithMedia.Media is EmbedExternal { External: { } embeddedExternal })
                    {
                        if (HasMutedWord(mutedWords, $"{embeddedExternal.Title} {embeddedExternal.Description}", [], [], [], embeddedAuthor))
                            return true;
                    }

                    if (recordWithMedia.Media is EmbedImages embeddedImages)
                    {
                        foreach (var image in embeddedImages.Images ?? [])
                        {
                            if (HasMutedWord(mutedWords, image.Alt ?? "", [], [], embeddedPost?.Langs ?? [], subject.Author))
                                return true;
                        }
                    }
                }
            }
            // link card
            else if (subject.Embed is ViewExternal { External: { } external })
            {
                if (HasMutedWord(mutedWords, $"{external.Title} {external.Description}", [], [], [], subject.Author))
                    return true;
            }
            // quite post with media
            else if (subject.Embed is ViewRecordWithMedia { Record.Record: ViewRecord embedRecord } viewRecordWithMedia)
            {
                var embedAuthor = embedRecord.Author;
                if (embedRecord.Value is Post embedPost)
                {
                    if (HasMutedWord(mutedWords, embedPost.Text ?? "", embedPost.Facets ?? [], embedPost.Tags ?? [], embedPost.Langs ?? [], embedAuthor))
                        return true;
                }

                if (viewRecordWithMedia.Media is ViewImages viewImages)
                {
                    foreach (var image in viewImages.Images ?? [])
                    {
                        if (HasMutedWord(mutedWords, image.Alt ?? "", [], [], subject.Record is Post p ? p.Langs ?? [] : [], embedAuthor))
                            return true;
                    }
                }

                if (viewRecordWithMedia.Media is ViewExternal { External: { } viewExternal })
                {
                    if (HasMutedWord(mutedWords, $"{viewExternal.Title} {viewExternal.Description}", [], [], [], embedAuthor))
                        return true;
                }
            }
        }

        return false;
    }

    private static readonly string[] LANGUAGE_EXCEPTIONS = [
        "ja", // Japanese
        "zh", // Chinese
        "ko", // Korean
        "th", // Thai
        "vi", // Vietnamese
    ];

    private static bool HasMutedWord(IReadOnlyList<MutedWord> mutedWords,
                                     string text,
                                     IReadOnlyList<Facet> facets,
                                     IReadOnlyList<string> outlineTags,
                                     IReadOnlyList<string> languages,
                                     ProfileViewBasic? actor)
    {
        if (mutedWords.Count == 0 || string.IsNullOrWhiteSpace(text))
            return false;

        string[] tags = [
            .. outlineTags.Select(s => s.ToUpperInvariant()),
            .. facets.SelectMany(f => f.Features.OfType<Tag>()).Select(t => t.TagValue!.ToUpperInvariant())];

        var exceptionalLanguage = languages.Any(l => LANGUAGE_EXCEPTIONS.Contains(l));
        var postText = text.ToUpperInvariant();
        var words = WordBoundary.Split(postText);
        foreach (var mute in mutedWords)
        {
            // expired
            if (mute.ExpiresAt != null && mute.ExpiresAt.Value < DateTime.Now)
                continue;

            // following
            if (mute.ActorTarget == "exclude-following" && actor?.Viewer?.Following != null)
                continue;

            var mutedWord = mute.Value!.ToUpperInvariant();

            // first check tags
            if (tags.Contains(mutedWord))
                return true;

            // the rest is content
            if (!mute.Targets!.Contains("content"))
                continue;

            if ((mutedWord.Length == 1 || exceptionalLanguage) && postText.Contains(mutedWord))
                return true;

            // too long
            if (mutedWord.Length > postText.Length)
                continue;

            // exact match
            if (mutedWord == postText)
                return true;

            // muted phrase contains a space or punctuation so we can't try to match this properly
            if (SpaceOrPunctuation.IsMatch(mutedWord) && postText.Contains(mutedWord))
                return true;

            foreach (var word in words)
            {
                if (word == mutedWord) return true;
                var wordTrimmedPunctuation = LeadingTrailingPunctuation.Replace(word, "");

                if (mutedWord.Length > wordTrimmedPunctuation.Length) continue;

                if (mutedWord == wordTrimmedPunctuation) return true;

                if (Punctuaton.IsMatch(wordTrimmedPunctuation))
                {
                    var spacedWord = Punctuaton.Replace(wordTrimmedPunctuation, "");
                    if (spacedWord == mutedWord) return true;

                    var contiguousWord = Whitespace.Replace(spacedWord, "");
                    if (contiguousWord == mutedWord) return true;

                    var wordParts = Punctuaton.Split(wordTrimmedPunctuation);
                    foreach (var wordPart in wordParts)
                        if (wordPart == mutedWord) return true;
                }
            }
        }

        return false;
    }
}
