using System.Net;
using System.Text;
using System.Text.Json;
using CommunityToolkit.WinUI.Notifications;
using FishyFlip;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using UniSky.Notifications.Data;
using UniSky.Notifications.Models;
using UniSky.Notifications.Models.WNS;
using UniSky.Notifications.Services.Providers;

namespace UniSky.Notifications.Services;

public class PushServiceWin10(
    ILogger<PushServiceWin10> logger,
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory) : IPushService
{

    private OAuthToken? cache;
    private async Task<OAuthToken> GetAccessTokens(bool invalidateCache = false)
    {
        if (cache != null && !invalidateCache)
            return cache;

        var clientId = configuration["WNS:ClientId"]!;
        var clientSecret = configuration["WNS:ClientSecret"]!;

        var parameters = new List<KeyValuePair<string, StringValues>>
        {
            new("grant_type", "client_credentials"),
            new("client_id", clientId),
            new("client_secret", clientSecret),
            new("scope", "notify.windows.com")
        };

        var query = QueryHelpers.AddQueryString("?", parameters)[1..];

        using var client = httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://login.live.com/accesstoken.srf");
        request.Content = new StringContent(query, Encoding.UTF8, "application/x-www-form-urlencoded");
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadAsStringAsync();
        var tokens = JsonSerializer.Deserialize(result, AppJsonSerializerContext.Default.OAuthToken)!;
        return cache = tokens;
    }

    public async Task<bool> PushNotificationAsync(ATProtocol at, NotificationEvent notificationEvent, INotificationProvider service, NotificationRegistration registration)
    {
        var tokens = await GetAccessTokens();
        var notification = new ToastContentBuilder()
                            .AddArgument("Type", notificationEvent.SourceCollection)
                            .AddArgument("Record", notificationEvent.SubjectRecordUri?.ToString());

        if (!await service.PopulateModernNotification(at, notificationEvent with { Registration = registration }, notification))
            return true;

        var notificationXml = notification
            .GetToastContent()
            .GetContent();

        if (!await SendNotificationAsync(notificationXml, tokens, registration))
            return false;

        return true;
    }

    private async Task<bool> SendNotificationAsync(string notificationXml, OAuthToken tokens, NotificationRegistration registration)
    {
        using var client = httpClientFactory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, registration.ChannelUrl);
        request.Headers.Add("Authorization", "Bearer " + tokens.AccessToken);
        request.Headers.Add("X-WNS-RequestForStatus", "true");
        request.Headers.Add("X-WNS-Type", "wns/toast");
        request.Content = new StringContent(notificationXml, Encoding.UTF8, "text/xml");

        try
        {
            using var response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
                return true;

            logger.LogWarning("Failed to post notification! {StatusCode}", response.StatusCode);

            switch (response.StatusCode)
            {
                case HttpStatusCode.Unauthorized:
                    tokens = await GetAccessTokens(true);
                    return await SendNotificationAsync(notificationXml, tokens, registration);
                case HttpStatusCode.Gone:
                case HttpStatusCode.NotFound:
                    return false;
                case HttpStatusCode.NotAcceptable:
                    break; // TODO: backoff
                default:
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send push notification to client!");
        }

        return true;
    }
}