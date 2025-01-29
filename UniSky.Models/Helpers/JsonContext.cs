using System.Text.Json.Serialization;
using FishyFlip.Tools.Json;
using UniSky.Models;
using UniSky.Moderation;

namespace UniSky.Helpers;

[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNameCaseInsensitive = true,
    Converters = new[] {
            typeof(FishyFlip.Tools.Json.ATUriJsonConverter),
            typeof(FishyFlip.Tools.Json.ATCidJsonConverter),
            typeof(FishyFlip.Tools.Json.ATHandleJsonConverter),
            typeof(FishyFlip.Tools.Json.ATDidJsonConverter),
            typeof(FishyFlip.Tools.Json.ATIdentifierJsonConverter),
            typeof(FishyFlip.Tools.Json.ATWebSocketCommitTypeConverter),
            typeof(FishyFlip.Tools.Json.ATWebSocketEventConverter),
            typeof(FishyFlip.Tools.Json.ATObjectJsonConverter),
    },
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull | JsonIgnoreCondition.WhenWritingDefault)]
[JsonSerializable(typeof(SessionModel))]
[JsonSerializable(typeof(ModerationCache))]
public partial class JsonContext : JsonSerializerContext
{
}
