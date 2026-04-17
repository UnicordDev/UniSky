using System.Text.Json.Serialization;

namespace UniSky.Notifications.Models.WNS;

public record class OAuthToken(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("token_type")] string TokenType
    );