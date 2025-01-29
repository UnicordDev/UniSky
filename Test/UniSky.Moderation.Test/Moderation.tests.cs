using FishyFlip.Lexicon.App.Bsky.Actor;
using FishyFlip.Lexicon.App.Bsky.Feed;
using FishyFlip.Lexicon.Com.Atproto.Label;
using FishyFlip.Lexicon.Tools.Ozone.Moderation;
using FishyFlip.Models;
using IdentityModel.OidcClient;

namespace UniSky.Moderation.Test;

public class ModerationTests
{
    [Fact]
    public void Applies_Self_Labels_On_Profiles_From_Global_Preferences_1()
    {
        var profileViewBasic = new ProfileViewBasic(
            did: new ATDid("did:web:bob.test"),
            handle: new ATHandle("bob.test"),
            labels: [
                new Label(
                    src: new ATDid("did:web:bob.test"),
                    uri: "at://did:web:bob.test/app.bsky.actor.profile/self",
                    val: "porn",
                    cts: DateTime.Now)
                ]);

        var options = new ModerationOptions(
            new ATDid("did:web:alice.test"),
            new ModerationPrefs(true, new Dictionary<string, LabelPreference>() { ["porn"] = LabelPreference.Hide }, [], [], []),
            [], []);

        var moderator = new Moderator(options);
        var result = moderator.ModerateProfile(profileViewBasic);
        var ui = result.GetUI(ModerationContext.Avatar);

        Assert.True(ui.Blur);
        Assert.False(ui.Alert);
        Assert.False(ui.Filter);
        Assert.False(ui.Inform);
    }

    [Fact]
    public void Applies_Self_Labels_On_Profiles_From_Global_Preferences_2()
    {
        var profileViewBasic = new ProfileViewBasic(
            did: new ATDid("did:web:bob.test"),
            handle: new ATHandle("bob.test"),
            labels: [
                new Label(
                    src: new ATDid("did:web:bob.test"),
                    uri: "at://did:web:bob.test/app.bsky.actor.profile/self",
                    val: "porn",
                    cts: DateTime.Now)
                ]);

        var options = new ModerationOptions(
            new ATDid("did:web:alice.test"),
            new ModerationPrefs(true, new Dictionary<string, LabelPreference>() { ["porn"] = LabelPreference.Ignore }, [], [], []),
            [], []);

        var moderator = new Moderator(options);
        var result = moderator.ModerateProfile(profileViewBasic);
        var ui = result.GetUI(ModerationContext.Avatar);

        Assert.False(ui.Blur);
        Assert.False(ui.Alert);
        Assert.False(ui.Filter);
        Assert.False(ui.Inform);
    }

    [Fact]
    public void Ignores_Labels_From_Unsubscribed_Moderators()
    {
        var profileViewBasic = new ProfileViewBasic(
            did: new ATDid("did:web:bob.test"),
            handle: new ATHandle("bob.test"),
            labels: [
                new Label(
                    src: new ATDid("did:web:labeler.test"),
                    uri: "at://did:web:bob.test/app.bsky.actor.profile/self",
                    val: "porn",
                    cts: DateTime.Now)
                ]);

        var options = new ModerationOptions(
            new ATDid("did:web:alice.test"),
            new ModerationPrefs(true, new Dictionary<string, LabelPreference>() { ["porn"] = LabelPreference.Hide }, [], [], []),
            [], []);

        var moderator = new Moderator(options);
        var result = moderator.ModerateProfile(profileViewBasic);
        foreach (var item in Enum.GetValues<ModerationContext>())
        {
            var ui = result.GetUI(item);
            Assert.False(ui.Blur);
            Assert.False(ui.Alert);
            Assert.False(ui.Filter);
            Assert.False(ui.Inform);
        }
    }

    [Fact]
    public void Ignores_Labels_From_Disabled_Groups()
    {
        var profileViewBasic = new ProfileViewBasic(
            did: new ATDid("did:web:bob.test"),
            handle: new ATHandle("bob.test"),
            labels: [
                new Label(
                    src: new ATDid("did:web:labeler.test"),
                    uri: "at://did:web:bob.test/app.bsky.actor.profile/self",
                    val: "porn",
                    cts: DateTime.Now)
            ]
        );

        var options = new ModerationOptions(
            new ATDid("did:web:alice.test"),
            new ModerationPrefs(true,
                new()
                {
                    ["porn"] = LabelPreference.Ignore
                },
                [
                    new ModerationPrefsLabeler(
                        new ATDid("did:web:labeler.test"),
                        new() { ["porn"] = LabelPreference.Ignore })
                ],
                [],
                []
            ),
            [],
            []
        );

        var moderator = new Moderator(options);
        var result = moderator.ModerateProfile(profileViewBasic);
        foreach (var item in Enum.GetValues<ModerationContext>())
        {
            var ui = result.GetUI(item);
            Assert.False(ui.Blur);
            Assert.False(ui.Alert);
            Assert.False(ui.Filter);
            Assert.False(ui.Inform);
        }
    }

    [Fact]
    public void Can_Manually_Apply_Hiding()
    {
        var postView = new PostView(
            record: new Post(text: "Hi!", createdAt: DateTime.Now),
            author: new ProfileViewBasic(did: new ATDid("did:web:bob.test"), handle: new ATHandle("bob.test"), displayName: "Bob"),
            labels: []);

        var options = new ModerationOptions(
            new ATDid("did:web:alice.test"),
            new ModerationPrefs(true,
                [],
                [new(new ATDid("did:web:labeler.test"), [])],
                [],
                []
            ),
            [],
            []
        );

        var moderator = new Moderator(options);
        var result = moderator.ModeratePost(postView);
        result.AddHidden(true);

        var contentList = result.GetUI(ModerationContext.ContentList);
        var contentView = result.GetUI(ModerationContext.ContentView);
        Helpers.
                AssertModerationUI(contentList, blur: true, filter: true);
        Helpers.AssertModerationUI(contentView, blur: true);
    }

    [Fact]
    public void Prioritizes_Filters_Correctly_On_Merge()
    {
        var postView = new PostView(
            record: new Post(text: "Hi!", createdAt: DateTime.Now),
            author: new ProfileViewBasic(did: new ATDid("did:web:bob.test"), handle: new ATHandle("bob.test"), displayName: "Bob"),
            labels: [
                new Label(src: new ATDid("did:web:labeler.test"), uri: "at://did:web:bob.test/app.bsky.post/fake", val: "porn", cts: DateTime.Now),
                new Label(src: new ATDid("did:web:labeler.test"), uri: "at://did:web:bob.test/app.bsky.post/fake", val: "!hide", cts: DateTime.Now),
                ]
            );

        var options = new ModerationOptions(
            new ATDid("did:web:alice.test"),
            new ModerationPrefs(true,
                new() { ["porn"] = LabelPreference.Hide },
                [new(new ATDid("did:web:labeler.test"), [])],
                [],
                []
            ),
            [],
            []
        );

        var moderator = new Moderator(options);
        var result = moderator.ModeratePost(postView);

        var contentList = result.GetUI(ModerationContext.ContentList);
        var contentMedia = result.GetUI(ModerationContext.ContentMedia);

        Assert.Equal(2, contentList.Filters.Count);
        Assert.Single(contentList.Blurs);
        Assert.Single(contentMedia.Blurs);
        Assert.IsType<LabelModerationCause>(contentList.Filters[0]);
        Assert.IsType<LabelModerationCause>(contentList.Filters[1]);
        Assert.IsType<LabelModerationCause>(contentList.Blurs[0]);
        Assert.IsType<LabelModerationCause>(contentMedia.Blurs[0]);
        Assert.Equal("!hide", ((LabelModerationCause)contentList.Filters[0]).Label.Val);
        Assert.Equal("porn", ((LabelModerationCause)contentList.Filters[1]).Label.Val);
        Assert.Equal("!hide", ((LabelModerationCause)contentList.Blurs[0]).Label.Val);
        Assert.Equal("porn", ((LabelModerationCause)contentMedia.Blurs[0]).Label.Val);
    }

    [Fact]
    public void Prioritizes_Custom_Label_Definitions()
    {
        var options = new ModerationOptions(
            new ATDid("did:web:alice.test"),
            new(
                adultContentEnabled: true,
                labels: new()
                {
                    ["porn"] = LabelPreference.Warn
                },
                labelers: [
                    new(
                        did: new ATDid("did:web:labeler.test"),
                        labels: new() {
                            ["porn"] = LabelPreference.Warn
                        }
                    )
                ],
                mutedWords: [],
                hiddenPosts: []
            ),
            [],
            new()
            {
                ["did:web:labeler.test"] = [
                    new InterpretedLabelValueDefinition(
                        new LabelValueDefinition(
                            identifier: "porn",
                            blurs: "none",
                            severity: "inform",
                            defaultSetting: "warn",
                            locales: []
                        ),
                        new ATDid("did:web:labeler.test")
                    )
                ]
            }
        );

        var postView = new PostView(
            record: new Post(text: "Hi!", createdAt: DateTime.Now),
            author: new ProfileViewBasic(did: new ATDid("did:web:bob.test"), handle: new ATHandle("bob.test"), displayName: "Bob"),
            labels: [
                new Label(src: new ATDid("did:web:labeler.test"), uri: "at://did:web:bob.test/app.bsky.post/fake", val: "porn", cts: DateTime.Now),
                ]
            );

        var moderator = new Moderator(options);
        var result = moderator.ModeratePost(postView);
        Helpers.
                AssertModerationUI(result.GetUI(ModerationContext.ProfileList));
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.ProfileView));
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.Avatar));
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.Banner));
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.DisplayName));
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.ContentList), inform: true);
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.ContentView), inform: true);
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.ContentMedia));
    }

    [Fact]
    public void Doesnt_Allow_Custom_Behaviours_To_Override_Imperative_Labels()
    {
        var options = new ModerationOptions(
            userDid: new ATDid("did:web:alice.test"),
            prefs: new ModerationPrefs(
                adultContentEnabled: true,
                labels: [],
                labelers: [
                    new(did: new ATDid("did:web:labeler.test"), labels: [])
                ],
                mutedWords: [],
                hiddenPosts: []
            ),
            labelers: [],
            labelDefs: new()
            {
                ["did:web:labeler.test"] = [
                    new InterpretedLabelValueDefinition(
                        new LabelValueDefinition(
                            identifier: "!hide",
                            blurs: "none",
                            severity: "inform",
                            defaultSetting: "warn",
                            locales: []
                        ),
                        new ATDid("did:web:labeler.test")
                    )
                ]
            }
        );

        var postView = new PostView(
            record: new Post(
                text: "Hi!",
                createdAt: DateTime.Now
            ),
            author: new ProfileViewBasic(
                did: new ATDid("did:web:bob.test"),
                handle: new ATHandle("bob.test"),
                displayName: "Bob"
            ),
            labels:
            [
                new Label(
                    src: new ATDid("did:web:labeler.test"),
                    uri: "at://did:web:bob.test/app.bsky.post/fake",
                    val: "!hide",
                    cts: DateTime.Now
                ),
            ]
        );


        var moderator = new Moderator(options);
        var result = moderator.ModeratePost(postView);
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.ProfileList));
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.ProfileView));
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.Avatar));
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.Banner));
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.DisplayName));
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.ContentList), filter: true, blur: true, noOverride: true);
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.ContentView), blur: true, noOverride: true);
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.ContentMedia));
    }

    [Fact]
    public void Ignores_Invalid_Label_Value_Names()
    {
        var options = new ModerationOptions(
            userDid: new ATDid("did:web:alice.test"),
            prefs: new ModerationPrefs(
                adultContentEnabled: true,
                labels: [],
                labelers: [
                    new(
                        did: new ATDid("did:web:labeler.test"),
                        labels: new() {
                            ["BadLabel"] = LabelPreference.Hide,
                            ["bad/label"] = LabelPreference.Hide
                        }
                    )
                ],
                mutedWords: [],
                hiddenPosts: []
            ),
            labelers: [],
            labelDefs: new()
            {
                ["did:web:labeler.test"] = [
                    new InterpretedLabelValueDefinition(
                        new LabelValueDefinition(
                            identifier: "BadLabel",
                            blurs: "content",
                            severity: "inform",
                            defaultSetting: "warn",
                            locales: []
                        ),
                        new ATDid("did:web:labeler.test")
                    ),
                    new InterpretedLabelValueDefinition(
                        new LabelValueDefinition(
                            identifier: "bad/label",
                            blurs: "content",
                            severity: "inform",
                            defaultSetting: "warn",
                            locales: []
                        ),
                        new ATDid("did:web:labeler.test")
                    )
                ]
            }
        );


        var postView = new PostView(
            record: new Post(
                text: "Hi!",
                createdAt: DateTime.Now
            ),
            author: new ProfileViewBasic(
                did: new ATDid("did:web:bob.test"),
                handle: new ATHandle("bob.test"),
                displayName: "Bob"
            ),
            labels:
            [
                new Label(
                    src: new ATDid("did:web:labeler.test"),
                    uri: "at://did:web:bob.test/app.bsky.post/fake",
                    val: "BadLabel",
                    cts: DateTime.Now
                ),
                new Label(
                    src: new ATDid("did:web:labeler.test"),
                    uri: "at://did:web:bob.test/app.bsky.post/fake",
                    val: "bad/label",
                    cts: DateTime.Now
                ),
            ]
        );

        var moderator = new Moderator(options);
        var result = moderator.ModeratePost(postView);
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.ProfileList));
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.ProfileView));
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.Avatar));
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.Banner));
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.DisplayName));
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.ContentList));
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.ContentView));
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.ContentMedia));
    }


    [Fact]
    public void Custom_Labels_Can_Set_Default_Setting()
    {
        var options = new ModerationOptions(
            userDid: new ATDid("did:web:alice.test"),
            prefs: new ModerationPrefs(
                adultContentEnabled: true,
                labels: [],
                labelers: [
                    new(
                        did: new ATDid("did:web:labeler.test"),
                        labels: new() {}
                    )
                ],
                mutedWords: [],
                hiddenPosts: []
            ),
            labelers: [],
            labelDefs: new()
            {
                ["did:web:labeler.test"] = [
                    new InterpretedLabelValueDefinition(
                        new LabelValueDefinition(
                            identifier: "default-hide",
                            blurs: "content",
                            severity: "inform",
                            defaultSetting: "hide",
                            locales: []
                        ),
                        new ATDid("did:web:labeler.test")
                    ),
                    new InterpretedLabelValueDefinition(
                        new LabelValueDefinition(
                            identifier: "default-warn",
                            blurs: "content",
                            severity: "inform",
                            defaultSetting: "warn",
                            locales: []
                        ),
                        new ATDid("did:web:labeler.test")
                    ),
                    new InterpretedLabelValueDefinition(
                        new LabelValueDefinition(
                            identifier: "default-ignore",
                            blurs: "content",
                            severity: "inform",
                            defaultSetting: "ignore",
                            locales: []
                        ),
                        new ATDid("did:web:labeler.test")
                    ),
                ]
            }
        );

        var hiddenPost = new PostView(
            record: new Post(
                text: "Hi!",
                createdAt: DateTime.Now
            ),
            author: new ProfileViewBasic(
                did: new ATDid("did:web:bob.test"),
                handle: new ATHandle("bob.test"),
                displayName: "Bob"
            ),
            labels: [
                new Label(
                    src: new ATDid("did:web:labeler.test"),
                    uri: "at://did:web:bob.test/app.bsky.post/fake",
                    val: "default-hide",
                    cts: DateTime.Now
                ),
            ]
        );

        var moderator = new Moderator(options);
        var result = moderator.ModeratePost(hiddenPost);
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.ProfileList));
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.ProfileView));
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.Avatar));
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.Banner));
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.DisplayName));
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.ContentList), filter: true, blur: true);
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.ContentView), inform: true);
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.ContentMedia));

        var warnPost = new PostView(
            record: new Post(
                text: "Hi!",
                createdAt: DateTime.Now
            ),
            author: new ProfileViewBasic(
                did: new ATDid("did:web:bob.test"),
                handle: new ATHandle("bob.test"),
                displayName: "Bob"
            ),
            labels: [
                new Label(
                    src: new ATDid("did:web:labeler.test"),
                    uri: "at://did:web:bob.test/app.bsky.post/fake",
                    val: "default-warn",
                    cts: DateTime.Now
                ),
            ]
        );

        result = moderator.ModeratePost(warnPost);
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.ProfileList));
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.ProfileView));
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.Avatar));
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.Banner));
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.DisplayName));
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.ContentList), blur: true);
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.ContentView), inform: true);
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.ContentMedia));

        var ignorePost = new PostView(
            record: new Post(
                text: "Hi!",
                createdAt: DateTime.Now
            ),
            author: new ProfileViewBasic(
                did: new ATDid("did:web:bob.test"),
                handle: new ATHandle("bob.test"),
                displayName: "Bob"
            ),
            labels: [
                new Label(
                    src: new ATDid("did:web:labeler.test"),
                    uri: "at://did:web:bob.test/app.bsky.post/fake",
                    val: "default-ignore",
                    cts: DateTime.Now
                ),
            ]
        );

        result = moderator.ModeratePost(ignorePost);
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.ProfileList));
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.ProfileView));
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.Avatar));
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.Banner));
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.DisplayName));
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.ContentList));
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.ContentView));
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.ContentMedia));
    }

    [Fact]
    public void Custom_Labels_Can_Require_Adult_Content_To_Be_Enabled()
    {
        var options = new ModerationOptions(
            userDid: new ATDid("did:web:alice.test"),
            prefs: new ModerationPrefs(
                adultContentEnabled: false,
                labels: [],
                labelers: [
                    new(
                        did: new ATDid("did:web:labeler.test"),
                        labels: new() {
                            ["adult"] = LabelPreference.Ignore
                        }
                    )
                ],
                mutedWords: [],
                hiddenPosts: []
            ),
            labelers: [],
            labelDefs: new()
            {
                ["did:web:labeler.test"] = [
                    new InterpretedLabelValueDefinition(
                        new LabelValueDefinition(
                            identifier: "adult",
                            blurs: "content",
                            severity: "inform",
                            defaultSetting: "hide",
                            adultOnly: true,
                            locales: []
                        ),
                        new ATDid("did:web:labeler.test")
                    )
                ]
            }
        );

        var postView = new PostView(
            record: new Post(
                text: "Hi!",
                createdAt: DateTime.Now
            ),
            author: new ProfileViewBasic(
                did: new ATDid("did:web:bob.test"),
                handle: new ATHandle("bob.test"),
                displayName: "Bob"
            ),
            labels: [
                new Label(
                    src: new ATDid("did:web:labeler.test"),
                    uri: "at://did:web:bob.test/app.bsky.post/fake",
                    val: "adult",
                    cts: DateTime.Now
                ),
            ]
        );

        var moderator = new Moderator(options);
        var result = moderator.ModeratePost(postView);
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.ProfileList));
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.ProfileView));
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.Avatar));
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.Banner));
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.DisplayName));
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.ContentList), filter: true, blur: true, noOverride: true);
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.ContentView), blur: true, noOverride: true);
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.ContentMedia));
    }

    [Fact]
    public void Adult_Content_Disabled_Forces_Hide()
    {
        var options = new ModerationOptions(
            userDid: new ATDid("did:web:alice.test"),
            prefs: new ModerationPrefs(
                adultContentEnabled: false,
                labels: new() { ["porn"] = LabelPreference.Ignore },
                labelers: [
                    new(
                        did: new ATDid("did:web:labeler.test"),
                        labels: []
                    )
                ],
                mutedWords: [],
                hiddenPosts: []
            ),
            labelers: [],
            labelDefs: []
        );

        var postView = new PostView(
            record: new Post(
                text: "Hi!",
                createdAt: DateTime.Now
            ),
            author: new ProfileViewBasic(
                did: new ATDid("did:web:bob.test"),
                handle: new ATHandle("bob.test"),
                displayName: "Bob"
            ),
            labels: [
                new Label(
                    src: new ATDid("did:web:labeler.test"),
                    uri: "at://did:web:bob.test/app.bsky.post/fake",
                    val: "porn",
                    cts: DateTime.Now
                ),
            ]
        );

        var moderator = new Moderator(options);
        var result = moderator.ModeratePost(postView);
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.ProfileList));
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.ProfileView));
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.Avatar));
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.Banner));
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.DisplayName));
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.ContentList), filter: true);
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.ContentView));
        Helpers.AssertModerationUI(result.GetUI(ModerationContext.ContentMedia), blur: true, noOverride: true);
    }
}