using FishyFlip.Lexicon.Com.Atproto.Label;

namespace UniSky.Moderation;

public class LabelModerationCause : ModerationCause
{
    public LabelModerationCause() : base()
    {
        Type = ModerationCauseType.Label;
    }

    public required Label Label { get; set; }
    public required InterpretedLabelValueDefinition LabelDef { get; set; }
    public required LabelTarget Target { get; set; }
    public required LabelPreference Setting { get; set; }
    public required ModerationBehavior Behavior { get; set; }
    public required bool NoOverride { get; set; }

    public override string ToString()
    {
        return $"{{ Type = {Type}, Label = {Label}, LabelDef = {LabelDef}, Target = {Target}, Setting = {Setting}, Behaviour = {Behavior}, NoOverride = {NoOverride} }}";
    }
}
