using System.Text.Json.Serialization;

namespace UniSky.Notifications.Models.Spacedust;

public record class SpacedustEvent(
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("origin")] string Origin,
    [property: JsonPropertyName("link")] SpacedustLinkEvent Link
    );

public record class SpacedustLinkEvent(
    [property: JsonPropertyName("operation")] string Operation,
    [property: JsonPropertyName("source")] string Source,
    [property: JsonPropertyName("source_record")] string SourceRecord,
    [property: JsonPropertyName("source_rev")] string SourceRevision,
    [property: JsonPropertyName("subject")] string Subject
    );