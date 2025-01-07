using System;

namespace UniSky.Moderation;

[Flags]
public enum LabelValueDefinitionFlags
{
    NoOverride = 1,
    Adult = 2,
    UnAuthed = 4,
    NoSelf = 8
};
