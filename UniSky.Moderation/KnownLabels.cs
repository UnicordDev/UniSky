using System;
using System.Collections.Frozen;
using System.Collections.Generic;

namespace UniSky.Moderation;

public enum KnownLabels
{
    Invalid = -1,
    Hide,
    Warn,
    NoUnauthenticated,
    Porn,
    Sexual,
    Nudity,
    GraphicMedia
}

public static class KnownLabelsHelper
{
    public static KnownLabels FromString(string str) => str switch
    {
        "!hide" => KnownLabels.Hide,
        "!warn" => KnownLabels.Warn,
        "!no-unauthenticated" => KnownLabels.NoUnauthenticated,
        "porn" => KnownLabels.Porn,
        "sexual" => KnownLabels.Sexual,
        "nudity" => KnownLabels.Nudity,
        "graphic-media" => KnownLabels.GraphicMedia,
        "gore" => KnownLabels.GraphicMedia,
        _ => KnownLabels.Invalid
    };

    public static string? ToString(KnownLabels label) => label switch
    {
        KnownLabels.Hide => "!hide",
        KnownLabels.Warn => "!warn",
        KnownLabels.NoUnauthenticated => "!no-unauthenticated",
        KnownLabels.Porn => "porn",
        KnownLabels.Sexual => "sexual",
        KnownLabels.Nudity => "nudity",
        KnownLabels.GraphicMedia => "graphic-media",
        _ => null
    };

    public static readonly IReadOnlyDictionary<KnownLabels, InterpretedLabelValueDefinition> Labels
        = new Dictionary<KnownLabels, InterpretedLabelValueDefinition>()
        {
            [KnownLabels.Hide] = new()
            {
                Identifier = "!hide",
                Configurable = false,
                DefaultSetting = LabelPreference.Hide,
                Flags = LabelValueDefinitionFlags.NoOverride | LabelValueDefinitionFlags.NoSelf,
                Severity = LabelSeverity.Alert,
                Blurs = LabelBlurs.Content,
                Behaviors = new ModerationBehaviors()
                {
                    Account = new ModerationBehavior()
                    {
                        ProfileList = ModerationBehaviorType.Blur,
                        ProfileView = ModerationBehaviorType.Blur,
                        Avatar = ModerationBehaviorType.Blur,
                        Banner = ModerationBehaviorType.Blur,
                        DisplayName = ModerationBehaviorType.Blur,
                        ContentList = ModerationBehaviorType.Blur,
                        ContentView = ModerationBehaviorType.Blur,
                    },
                    Profile = new ModerationBehavior()
                    {
                        Avatar = ModerationBehaviorType.Blur,
                        Banner = ModerationBehaviorType.Blur,
                        DisplayName = ModerationBehaviorType.Blur,
                    },
                    Content = new ModerationBehavior()
                    {
                        ContentList = ModerationBehaviorType.Blur,
                        ContentView = ModerationBehaviorType.Blur,
                    }
                }
            },
            [KnownLabels.Warn] = new()
            {
                Identifier = "!warn",
                Configurable = false,
                DefaultSetting = LabelPreference.Warn,
                Flags = LabelValueDefinitionFlags.NoSelf,
                Severity = LabelSeverity.None,
                Blurs = LabelBlurs.Content,
                Behaviors = new ModerationBehaviors()
                {
                    Account = new ModerationBehavior()
                    {
                        ProfileList = ModerationBehaviorType.Blur,
                        ProfileView = ModerationBehaviorType.Blur,
                        Avatar = ModerationBehaviorType.Blur,
                        Banner = ModerationBehaviorType.Blur,
                        ContentList = ModerationBehaviorType.Blur,
                        ContentView = ModerationBehaviorType.Blur,
                    },
                    Profile = new ModerationBehavior()
                    {
                        Avatar = ModerationBehaviorType.Blur,
                        Banner = ModerationBehaviorType.Blur,
                        DisplayName = ModerationBehaviorType.Blur,
                    },
                    Content = new ModerationBehavior()
                    {
                        ContentList = ModerationBehaviorType.Blur,
                        ContentView = ModerationBehaviorType.Blur,
                    }
                }
            },
            [KnownLabels.NoUnauthenticated] = new()
            {
                Identifier = "!no-unauthenticated",
                Configurable = false,
                DefaultSetting = LabelPreference.Hide,
                Flags = LabelValueDefinitionFlags.NoOverride | LabelValueDefinitionFlags.UnAuthed,
                Severity = LabelSeverity.None,
                Blurs = LabelBlurs.Content,
                Behaviors = new ModerationBehaviors()
                {
                    Account = new ModerationBehavior()
                    {
                        ProfileList = ModerationBehaviorType.Blur,
                        ProfileView = ModerationBehaviorType.Blur,
                        Avatar = ModerationBehaviorType.Blur,
                        Banner = ModerationBehaviorType.Blur,
                        DisplayName = ModerationBehaviorType.Blur,
                        ContentList = ModerationBehaviorType.Blur,
                        ContentView = ModerationBehaviorType.Blur,
                    },
                    Profile = new ModerationBehavior()
                    {
                        Avatar = ModerationBehaviorType.Blur,
                        Banner = ModerationBehaviorType.Blur,
                        DisplayName = ModerationBehaviorType.Blur,
                    },
                    Content = new ModerationBehavior()
                    {
                        ContentList = ModerationBehaviorType.Blur,
                        ContentView = ModerationBehaviorType.Blur,
                    }
                }
            },
            [KnownLabels.Porn] = new()
            {
                Identifier = "porn",
                Configurable = true,
                DefaultSetting = LabelPreference.Warn,
                Flags = LabelValueDefinitionFlags.Adult,
                Severity = LabelSeverity.None,
                Blurs = LabelBlurs.Media,
                Behaviors = new ModerationBehaviors()
                {
                    Account = new ModerationBehavior()
                    {
                        Avatar = ModerationBehaviorType.Blur,
                        Banner = ModerationBehaviorType.Blur,
                    },
                    Profile = new ModerationBehavior()
                    {
                        Avatar = ModerationBehaviorType.Blur,
                        Banner = ModerationBehaviorType.Blur,
                    },
                    Content = new ModerationBehavior()
                    {
                        ContentMedia = ModerationBehaviorType.Blur
                    }
                }
            },
            [KnownLabels.Sexual] = new()
            {
                Identifier = "sexual",
                Configurable = true,
                DefaultSetting = LabelPreference.Warn,
                Flags = LabelValueDefinitionFlags.Adult,
                Severity = LabelSeverity.None,
                Blurs = LabelBlurs.Media,
                Behaviors = new ModerationBehaviors()
                {
                    Account = new ModerationBehavior()
                    {
                        Avatar = ModerationBehaviorType.Blur,
                        Banner = ModerationBehaviorType.Blur,
                    },
                    Profile = new ModerationBehavior()
                    {
                        Avatar = ModerationBehaviorType.Blur,
                        Banner = ModerationBehaviorType.Blur,
                    },
                    Content = new ModerationBehavior()
                    {
                        ContentMedia = ModerationBehaviorType.Blur
                    }
                }
            },
            [KnownLabels.Nudity] = new()
            {
                Identifier = "nudity",
                Configurable = true,
                DefaultSetting = LabelPreference.Ignore,
                Flags = 0,
                Severity = LabelSeverity.None,
                Blurs = LabelBlurs.Media,
                Behaviors = new ModerationBehaviors()
                {
                    Account = new ModerationBehavior()
                    {
                        Avatar = ModerationBehaviorType.Blur,
                        Banner = ModerationBehaviorType.Blur,
                    },
                    Profile = new ModerationBehavior()
                    {
                        Avatar = ModerationBehaviorType.Blur,
                        Banner = ModerationBehaviorType.Blur,
                    },
                    Content = new ModerationBehavior()
                    {
                        ContentMedia = ModerationBehaviorType.Blur
                    }
                }
            },
            [KnownLabels.GraphicMedia] = new()
            {
                Identifier = "graphic-media",
                Configurable = true,
                DefaultSetting = LabelPreference.Warn,
                Flags = LabelValueDefinitionFlags.Adult,
                Severity = LabelSeverity.None,
                Blurs = LabelBlurs.Media,
                Behaviors = new ModerationBehaviors()
                {
                    Account = new ModerationBehavior()
                    {
                        Avatar = ModerationBehaviorType.Blur,
                        Banner = ModerationBehaviorType.Blur,
                    },
                    Profile = new ModerationBehavior()
                    {
                        Avatar = ModerationBehaviorType.Blur,
                        Banner = ModerationBehaviorType.Blur,
                    },
                    Content = new ModerationBehavior()
                    {
                        ContentMedia = ModerationBehaviorType.Blur
                    }
                }
            }
        }.ToFrozenDictionary();
}
