namespace UniSky.Moderation;

public class ModerationCause
{
    public required ModerationCauseType Type { get; set; }
    public required ModerationCauseSource Source { get; set; }
    public required byte Priority { get; set; }
    public bool Downgraded { get; internal set; }

    public override string ToString()
    {
        return $"{{ Type = {Type}, Source = {Source}, Priority = {Priority}, Downgraded = {Downgraded} }}";
    }
}
