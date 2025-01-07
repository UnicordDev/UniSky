using System.Collections.Generic;
using System.Linq;

namespace UniSky.Moderation;

public readonly struct ModerationUI(
    bool noOverride,
    IList<ModerationCause> filters,
    IList<ModerationCause> blurs,
    IList<ModerationCause> alerts,
    IList<ModerationCause> informs)
{
    public IReadOnlyList<ModerationCause> Filters { get; } 
        = [.. filters.OrderBy(p => p.Priority)];
    public IReadOnlyList<ModerationCause> Blurs { get; }
        = [.. blurs.OrderBy(p => p.Priority)];
    public IReadOnlyList<ModerationCause> Alerts { get; } 
        = [.. alerts.OrderBy(p => p.Priority)];
    public IReadOnlyList<ModerationCause> Informs { get; } 
        = [.. informs.OrderBy(p => p.Priority)];

    /// <summary>
    /// If <see cref="Blur"/> is <see langword="true"/>, should the UI disable opening the cover?
    /// </summary>
    public bool NoOverride { get; }
        = noOverride;

    /// <summary>
    /// Should the content be removed from the UI entirely?
    /// </summary>
    public bool Filter
        => Filters.Count > 0;

    /// <summary>
    /// Should the content be put behind a cover or blurred?
    /// </summary>
    public bool Blur
        => Blurs.Count > 0;

    /// <summary>
    /// Should an alert be put on the content?
    /// </summary>
    public bool Alert
        => Alerts.Count > 0;

    /// <summary>
    /// Should an informational notice be put on the content?
    /// </summary>
    public bool Inform
        => Informs.Count > 0;
}
