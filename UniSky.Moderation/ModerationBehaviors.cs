using System;
using System.Text.Json.Serialization;

namespace UniSky.Moderation;

public struct ModerationBehaviors
{
    public readonly ModerationBehavior this[LabelTarget target]
        => target switch
        {
            LabelTarget.Account => Account,
            LabelTarget.Profile => Profile,
            LabelTarget.Content => Content,
            _ => throw new InvalidOperationException()
        };

    [JsonInclude]
    public ModerationBehavior Account;
    [JsonInclude]
    public ModerationBehavior Profile;
    [JsonInclude]
    public ModerationBehavior Content;
}
