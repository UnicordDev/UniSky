using System.Text.Json.Serialization;
using UniSky.Models;
using UniSky.Notifications.Models.Spacedust;
using UniSky.Notifications.Models.WNS;

namespace UniSky.Notifications;

[JsonSerializable(typeof(RegistrationModel))]
[JsonSerializable(typeof(SpacedustEvent))]
[JsonSerializable(typeof(OAuthToken))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}
