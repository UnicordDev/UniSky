using System.Collections.Generic;
using System.Text.Json.Serialization;
using FishyFlip.Lexicon.App.Bsky.Actor;
using FishyFlip.Lexicon.App.Bsky.Labeler;
using FishyFlip.Lexicon.Com.Atproto.Label;
using FishyFlip.Models;

namespace UniSky.Moderation;

public enum LabelSeverity { Inform, Alert, None }
public enum LabelBlurs { Content, Media, None }


public class InterpretedLabelValueDefinition
{
    public InterpretedLabelValueDefinition()
    {
        Identifier = "";
        Locales = [];
    }

    public InterpretedLabelValueDefinition(LabelValueDefinition def, ATDid definedBy, ProfileView? definedByProfile = null)
    {
        var behaviors = new ModerationBehaviors();
        var alertOrInform = def.Severity switch
        {
            "alert" => ModerationBehaviorType.Alert,
            "inform" => ModerationBehaviorType.Inform,
            _ => ModerationBehaviorType.None
        };

        if (def.Blurs == "content")
        {
            // target=account, blurs=content
            behaviors.Account.ProfileList = alertOrInform;
            behaviors.Account.ProfileView = alertOrInform;
            behaviors.Account.ContentList = ModerationBehaviorType.Blur;
            behaviors.Account.ContentView = def.AdultOnly == true ? ModerationBehaviorType.Blur : alertOrInform;
            // target=profile, blurs=content;=
            behaviors.Profile.ProfileList = alertOrInform;
            behaviors.Profile.ProfileView = alertOrInform;
            // target=content, blurs=content
            behaviors.Content.ContentList = ModerationBehaviorType.Blur;
            behaviors.Content.ContentView = def.AdultOnly == true ? ModerationBehaviorType.Blur : alertOrInform;
        }
        else if (def.Blurs == "media")
        {
            // target=account, blurs=media
            behaviors.Account.ProfileList = alertOrInform;
            behaviors.Account.ProfileView = alertOrInform;
            behaviors.Account.Avatar = ModerationBehaviorType.Blur;
            behaviors.Account.Banner = ModerationBehaviorType.Blur;
            // target=profile, blurs=media
            behaviors.Profile.ProfileList = alertOrInform;
            behaviors.Profile.ProfileView = alertOrInform;
            behaviors.Profile.Avatar = ModerationBehaviorType.Blur;
            behaviors.Profile.Banner = ModerationBehaviorType.Blur;
            // target=content, blurs=media
            behaviors.Content.ContentMedia = ModerationBehaviorType.Blur;
        }
        else if (def.Blurs == "none")
        {
            // target=account, blurs=none
            behaviors.Account.ProfileList = alertOrInform;
            behaviors.Account.ProfileView = alertOrInform;
            behaviors.Account.ContentList = alertOrInform;
            behaviors.Account.ContentView = alertOrInform;
            // target=profile, blurs=none
            behaviors.Profile.ProfileList = alertOrInform;
            behaviors.Profile.ProfileView = alertOrInform;
            // target=content, blurs=none
            behaviors.Content.ContentList = alertOrInform;
            behaviors.Content.ContentView = alertOrInform;
        }

        var defaultSetting = def.DefaultSetting switch
        {
            "hide" => LabelPreference.Hide,
            "ignore" => LabelPreference.Ignore,
            { } => LabelPreference.Warn,
            _ => (LabelPreference?)null
        };

        var flags = LabelValueDefinitionFlags.NoSelf;
        if (def.AdultOnly == true)
            flags |= LabelValueDefinitionFlags.Adult;

        Identifier = def.Identifier!;
        Severity = def.Severity switch
        {
            "alert" => LabelSeverity.Alert,
            "inform" => LabelSeverity.Inform,
            _ => LabelSeverity.None
        };
        Blurs = def.Blurs switch
        {
            "content" => LabelBlurs.Content,
            "media" => LabelBlurs.Media,
            _ => LabelBlurs.None
        };
        DefinedBy = definedBy;
        DefinedByProfile = definedByProfile;
        Configurable = true;
        AdultOnly = def.AdultOnly ?? false;
        DefaultSetting = defaultSetting;
        Flags = flags;
        Behaviors = behaviors;
        Locales = def.Locales != null ? [.. def.Locales] : [];
    }

    public string Identifier { get; init; }
    public LabelSeverity Severity { get; init; }
    public LabelBlurs Blurs { get; init; }
    public ATDid? DefinedBy { get; init; }
    public ProfileView? DefinedByProfile { get; init; }
    public bool Configurable { get; init; }
    public bool AdultOnly { get; init; }
    public LabelPreference? DefaultSetting { get; init; }
    public LabelValueDefinitionFlags Flags { get; init; }
    public ModerationBehaviors Behaviors { get; init; }
    public IReadOnlyList<LabelValueDefinitionStrings> Locales { get; set; }
}
