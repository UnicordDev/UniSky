namespace UniSky.Moderation;

public class ModerationCause
{
    public ModerationCauseType Type { get; internal set; }
    public ModerationCauseSource Source { get; internal set; }
    public byte Priority { get; internal set; }
    public bool Downgraded { get; internal set; }

    public override string ToString()
    {
        return $"{{ Type = {Type}, Source = {Source}, Priority = {Priority}, Downgraded = {Downgraded} }}";
    }
}
