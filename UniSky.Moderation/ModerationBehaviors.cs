using System;

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

    public ModerationBehavior Account;
    public ModerationBehavior Profile;
    public ModerationBehavior Content;
}
