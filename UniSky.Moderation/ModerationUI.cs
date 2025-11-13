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
    public IReadOnlyList<ModerationCause> Filters 
        => [.. filters.OrderBy(p => p.Priority)];
    public IReadOnlyList<ModerationCause> Blurs 
        => [.. blurs.OrderBy(p => p.Priority)];
    public IReadOnlyList<ModerationCause> Alerts 
        => [.. alerts.OrderBy(p => p.Priority)];
    public IReadOnlyList<ModerationCause> Informs 
        => [.. informs.OrderBy(p => p.Priority)];

    /// <summary>
    /// If <see cref="Blur"/> is <see langword="true"/>, should the UI disable opening the cover?
    /// </summary>
    public bool NoOverride { get; }
        = noOverride;

    /// <summary>
    /// Should the content be removed from the UI entirely?
    /// </summary>
    public bool Filter
        => filters.Count > 0;

    /// <summary>
    /// Should the content be put behind a cover or blurred?
    /// </summary>
    public bool Blur
        => blurs.Count > 0;

    /// <summary>
    /// Should an alert be put on the content?
    /// </summary>
    public bool Alert
        => alerts.Count > 0;

    /// <summary>
    /// Should an informational notice be put on the content?
    /// </summary>
    public bool Inform
        => informs.Count > 0;
}
