using FishyFlip.Lexicon.Com.Atproto.Label;

namespace UniSky.Moderation;

public class LabelModerationCause : ModerationCause
{
    public LabelModerationCause() : base()
    {
        Type = ModerationCauseType.Label;
    }

    public Label Label { get; internal set; }
    public InterpretedLabelValueDefinition LabelDef { get; internal set; }
    public LabelTarget Target { get; internal set; }
    public LabelPreference Setting { get; internal set; }
    public ModerationBehavior Behavior { get; internal set; }
    public bool NoOverride { get; internal set; }

    public override string ToString()
    {
        return $"{{ Type = {Type}, Label = {Label}, LabelDef = {LabelDef}, Target = {Target}, Setting = {Setting}, Behaviour = {Behavior}, NoOverride = {NoOverride} }}";
    }
}
