using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FishyFlip.Lexicon.App.Bsky.Actor;
using FishyFlip.Lexicon.App.Bsky.Feed;
using FishyFlip.Lexicon.App.Bsky.Richtext;
using FishyFlip.Models;

namespace UniSky.Moderation.Test;

public class MutedWordsTests
{
    [Collection("Tags")]
    public class Tags
    {
        [Fact]
        public void Matches_OutlineTag()
        {
            var options = new ModerationOptions(
                userDid: new ATDid("did:web:alice.test"),
                prefs: new ModerationPrefs(
                    adultContentEnabled: false,
                    labels: [],
                    labelers: [],
                    mutedWords: [
                        new MutedWord(value: "outlineTag", targets: ["tag"], actorTarget: "all")
                    ],
                    hiddenPosts: []
                ),
                labelDefs: []
            );

            var postView = new PostView(
                record: new Post(
                    text: "This is a post #inlineTag",
                    facets: [
                        new Facet(
                            index: new ByteSlice(15, 25),
                            features: [ new Tag("inlineTag") ]
                        )
                    ],
                    createdAt: DateTime.Now,
                    tags: ["outlineTag"]
                ),
                author: new ProfileViewBasic(
                    did: new ATDid("did:web:bob.test"),
                    handle: new ATHandle("bob.test"),
                    displayName: "Bob"
                ),
                labels: []
            );

            var moderator = new Moderator(options);
            var result = moderator.ModeratePost(postView);
            var ui = result.GetUI(ModerationContext.ContentList);

            Helpers.AssertModerationUI(ui, filter: true, blur: true);
        }

        [Fact]
        public void Matches_InlineTag()
        {
            var options = new ModerationOptions(
                userDid: new ATDid("did:web:alice.test"),
                prefs: new ModerationPrefs(
                    adultContentEnabled: false,
                    labels: [],
                    labelers: [],
                    mutedWords: [
                        new MutedWord(value: "inlineTag", targets: ["tag"], actorTarget: "all")
                    ],
                    hiddenPosts: []
                ),
                labelDefs: []
            );

            var postView = new PostView(
                record: new Post(
                    text: "This is a post #inlineTag",
                    facets: [
                        new Facet(
                            index: new ByteSlice(15, 25),
                            features: [ new Tag("inlineTag") ]
                        )
                    ],
                    createdAt: DateTime.Now,
                    tags: ["outlineTag"]
                ),
                author: new ProfileViewBasic(
                    did: new ATDid("did:web:bob.test"),
                    handle: new ATHandle("bob.test"),
                    displayName: "Bob"
                ),
                labels: []
            );

            var moderator = new Moderator(options);
            var result = moderator.ModeratePost(postView);
            var ui = result.GetUI(ModerationContext.ContentList);

            Helpers.AssertModerationUI(ui, filter: true, blur: true);
        }

        [Fact]
        public void Content_Target_Matches_InlineTag()
        {
            var options = new ModerationOptions(
                userDid: new ATDid("did:web:alice.test"),
                prefs: new ModerationPrefs(
                    adultContentEnabled: false,
                    labels: [],
                    labelers: [],
                    mutedWords: [
                        new MutedWord(value: "inlineTag", targets: ["content"], actorTarget: "all")
                    ],
                    hiddenPosts: []
                ),
                labelDefs: []
            );

            var postView = new PostView(
                record: new Post(
                    text: "This is a post #inlineTag",
                    facets: [
                        new Facet(
                            index: new ByteSlice(15, 25),
                            features: [ new Tag("inlineTag") ]
                        )
                    ],
                    createdAt: DateTime.Now,
                    tags: ["outlineTag"]
                ),
                author: new ProfileViewBasic(
                    did: new ATDid("did:web:bob.test"),
                    handle: new ATHandle("bob.test"),
                    displayName: "Bob"
                ),
                labels: []
            );

            var moderator = new Moderator(options);
            var result = moderator.ModeratePost(postView);
            var ui = result.GetUI(ModerationContext.ContentList);

            Helpers.AssertModerationUI(ui, filter: true, blur: true);
        }
    }

    [Collection("Early exits")]
    public class EarlyExits
    {
        [Fact]
        public void Match_SingleCharacter_Japanese()
        {
            var options = new ModerationOptions(
                userDid: new ATDid("did:web:alice.test"),
                prefs: new ModerationPrefs(
                    adultContentEnabled: false,
                    labels: [],
                    labelers: [],
                    mutedWords: [
                        new MutedWord(value: "希", targets: ["content"], actorTarget: "all")
                    ],
                    hiddenPosts: []
                ),
                labelDefs: []
            );

            var postView = new PostView(
                record: new Post(
                    text: "改善希望です",
                    facets: [],
                    createdAt: DateTime.Now,
                    tags: []
                ),
                author: new ProfileViewBasic(
                    did: new ATDid("did:web:bob.test"),
                    handle: new ATHandle("bob.test"),
                    displayName: "Bob"
                ),
                labels: []
            );

            var moderator = new Moderator(options);
            var result = moderator.ModeratePost(postView);
            var ui = result.GetUI(ModerationContext.ContentList);

            Helpers.AssertModerationUI(ui, filter: true, blur: true);
        }

        [Fact]
        public void Match_SingleCharacter_Emoji()
        {
            var options = new ModerationOptions(
                userDid: new ATDid("did:web:alice.test"),
                prefs: new ModerationPrefs(
                    adultContentEnabled: false,
                    labels: [],
                    labelers: [],
                    mutedWords: [
                        new MutedWord(value: "☠︎", targets: ["content"], actorTarget: "all")
                    ],
                    hiddenPosts: []
                ),
                labelDefs: []
            );

            var postView = new PostView(
                record: new Post(
                    text: "Idk why ☠︎ but maybe",
                    facets: [],
                    createdAt: DateTime.Now,
                    tags: []
                ),
                author: new ProfileViewBasic(
                    did: new ATDid("did:web:bob.test"),
                    handle: new ATHandle("bob.test"),
                    displayName: "Bob"
                ),
                labels: []
            );

            var moderator = new Moderator(options);
            var result = moderator.ModeratePost(postView);
            var ui = result.GetUI(ModerationContext.ContentList);

            Helpers.AssertModerationUI(ui, filter: true, blur: true);
        }

        [Fact]
        public void NoMatch_Long_Word_Short_Post()
        {
            var options = new ModerationOptions(
                userDid: new ATDid("did:web:alice.test"),
                prefs: new ModerationPrefs(
                    adultContentEnabled: false,
                    labels: [],
                    labelers: [],
                    mutedWords: [
                        new MutedWord(value: "politics", targets: ["content"], actorTarget: "all")
                    ],
                    hiddenPosts: []
                ),
                labelDefs: []
            );

            var postView = new PostView(
                record: new Post(
                    text: "hi",
                    facets: [],
                    createdAt: DateTime.Now,
                    tags: []
                ),
                author: new ProfileViewBasic(
                    did: new ATDid("did:web:bob.test"),
                    handle: new ATHandle("bob.test"),
                    displayName: "Bob"
                ),
                labels: []
            );

            var moderator = new Moderator(options);
            var result = moderator.ModeratePost(postView);
            var ui = result.GetUI(ModerationContext.ContentList);

            Helpers.AssertModerationUI(ui);
        }

        [Fact]
        public void Match_ExactMatch()
        {
            var options = new ModerationOptions(
                userDid: new ATDid("did:web:alice.test"),
                prefs: new ModerationPrefs(
                    adultContentEnabled: false,
                    labels: [],
                    labelers: [],
                    mutedWords: [
                        new MutedWord(value: "rust", targets: ["content"], actorTarget: "all")
                    ],
                    hiddenPosts: []
                ),
                labelDefs: []
            );

            var postView = new PostView(
               record: new Post(
                   text: "rust",
                   facets: [],
                   createdAt: DateTime.Now,
                   tags: []
               ),
               author: new ProfileViewBasic(
                   did: new ATDid("did:web:bob.test"),
                   handle: new ATHandle("bob.test"),
                   displayName: "Bob"
               ),
               labels: []
           );

            var moderator = new Moderator(options);
            var result = moderator.ModeratePost(postView);
            var ui = result.GetUI(ModerationContext.ContentList);

            Helpers.AssertModerationUI(ui, filter: true, blur: true);
        }
    }


    [Collection("Content")]
    public class Content
    {
        [Fact]
        public void Match_Word_Within_Post()
        {
            var options = new ModerationOptions(
                userDid: new ATDid("did:web:alice.test"),
                prefs: new ModerationPrefs(
                    adultContentEnabled: false,
                    labels: [],
                    labelers: [],
                    mutedWords: [
                        new MutedWord(value: "rust", targets: ["content"], actorTarget: "all")
                    ],
                    hiddenPosts: []
                ),
                labelDefs: []
            );

            var postView = new PostView(
               record: new Post(
                   text: "This post is about rust",
                   facets: [],
                   createdAt: DateTime.Now,
                   tags: []
               ),
               author: new ProfileViewBasic(
                   did: new ATDid("did:web:bob.test"),
                   handle: new ATHandle("bob.test"),
                   displayName: "Bob"
               ),
               labels: []
           );

            var moderator = new Moderator(options);
            var result = moderator.ModeratePost(postView);
            var ui = result.GetUI(ModerationContext.ContentList);

            Helpers.AssertModerationUI(ui, filter: true, blur: true);
        }

        [Fact]
        public void NoMatch_PartialWord()
        {
            var options = new ModerationOptions(
                userDid: new ATDid("did:web:alice.test"),
                prefs: new ModerationPrefs(
                    adultContentEnabled: false,
                    labels: [],
                    labelers: [],
                    mutedWords: [
                        new MutedWord(value: "ai", targets: ["content"], actorTarget: "all")
                    ],
                    hiddenPosts: []
                ),
                labelDefs: []
            );

            var postView = new PostView(
               record: new Post(
                   text: "Use your brain, Eric",
                   facets: [],
                   createdAt: DateTime.Now,
                   tags: []
               ),
               author: new ProfileViewBasic(
                   did: new ATDid("did:web:bob.test"),
                   handle: new ATHandle("bob.test"),
                   displayName: "Bob"
               ),
               labels: []
           );

            var moderator = new Moderator(options);
            var result = moderator.ModeratePost(postView);
            var ui = result.GetUI(ModerationContext.ContentList);

            Helpers.AssertModerationUI(ui);
        }

        [Fact]
        public void Match_Multiline()
        {
            var options = new ModerationOptions(
                userDid: new ATDid("did:web:alice.test"),
                prefs: new ModerationPrefs(
                    adultContentEnabled: false,
                    labels: [],
                    labelers: [],
                    mutedWords: [
                        new MutedWord(value: "brain", targets: ["content"], actorTarget: "all")
                    ],
                    hiddenPosts: []
                ),
                labelDefs: []
            );

            var postView = new PostView(
               record: new Post(
                   text: "Use your\n\tbrain, Eric",
                   facets: [],
                   createdAt: DateTime.Now,
                   tags: []
               ),
               author: new ProfileViewBasic(
                   did: new ATDid("did:web:bob.test"),
                   handle: new ATHandle("bob.test"),
                   displayName: "Bob"
               ),
               labels: []
           );

            var moderator = new Moderator(options);
            var result = moderator.ModeratePost(postView);
            var ui = result.GetUI(ModerationContext.ContentList);

            Helpers.AssertModerationUI(ui, filter: true, blur: true);
        }

        [Fact]
        public void Match_Smiley()
        {
            var options = new ModerationOptions(
                userDid: new ATDid("did:web:alice.test"),
                prefs: new ModerationPrefs(
                    adultContentEnabled: false,
                    labels: [],
                    labelers: [],
                    mutedWords: [
                        new MutedWord(value: ":)", targets: ["content"], actorTarget: "all")
                    ],
                    hiddenPosts: []
                ),
                labelDefs: []
            );

            var postView = new PostView(
               record: new Post(
                   text: "Ur cute :)",
                   facets: [],
                   createdAt: DateTime.Now,
                   tags: []
               ),
               author: new ProfileViewBasic(
                   did: new ATDid("did:web:bob.test"),
                   handle: new ATHandle("bob.test"),
                   displayName: "Bob"
               ),
               labels: []
           );

            var moderator = new Moderator(options);
            var result = moderator.ModeratePost(postView);
            var ui = result.GetUI(ModerationContext.ContentList);

            Helpers.AssertModerationUI(ui, filter: true, blur: true);
        }
    }
}
