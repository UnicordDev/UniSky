using System.Text.Json.Serialization;
using FishyFlip.Tools.Json;
using UniSky.Models;
using UniSky.Moderation;

namespace UniSky.Helpers;

[JsonSerializable(typeof(SessionModel))]
[JsonSerializable(typeof(ModerationCache))]
[JsonSerializable(typeof(RegistrationModel))]
[JsonSourceGenerationOptions(WriteIndented = false, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
public partial class JsonContext : JsonSerializerContext
{
}
