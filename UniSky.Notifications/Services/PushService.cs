
using System.Net;
using System.Text;
using System.Text.Json;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI.Notifications;
using FishyFlip;
using FishyFlip.Lexicon.App.Bsky.Actor;
using FishyFlip.Lexicon.App.Bsky.Feed;
using FishyFlip.Lexicon.Com.Atproto.Admin;
using FishyFlip.Lexicon.Com.Atproto.Repo;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using UniSky.Notifications.Data;
using UniSky.Notifications.Messages;
using UniSky.Notifications.Models;
using UniSky.Notifications.Models.WNS;
using UniSky.Notifications.Services.Providers;

namespace UniSky.Notifications.Services;

public class PushService(
    IConfiguration configuration,
    ILogger<PushService> logger,
    ILogger<ATProtocol> protocolLogger,
    IHttpClientFactory httpClientFactory,
    IServiceProvider services) : IHostedService, IRecipient<NotificationEventMessage>
{
    private ATProtocol at = new ATProtocolBuilder()
        .WithLogger(protocolLogger)
        .EnableBlueskyModerationService()
        .Build();

    public async Task OnNotificationEvent(NotificationEvent notificationEvent)
    {
        try
        {
            logger.LogInformation("Got event {Event}", notificationEvent.SourceType);
            var service = services.GetKeyedService<INotificationProvider>(notificationEvent.SourceType);
            if (service == null)
                return;

            var notification = new ToastContentBuilder()
                .AddArgument("Type", notificationEvent.SourceCollection)
                .AddArgument("Record", notificationEvent.SubjectRecordUri?.ToString());

            if (!await service.PopulateNotification(at, notificationEvent, notification))
                return;

            var notificationXml = notification
                .GetToastContent()
                .GetContent();

            var registrations = await GetRegistrationsAsync(notificationEvent);
            logger.LogInformation("Got {N} registrations for DID {DID}", registrations.Count, notificationEvent.SubjectDid);

            var tokens = await GetAccessTokens();
            await Task.WhenAll(registrations.Select(reg => SendNotificationAsync(notificationXml, tokens, reg)));
        }
        catch(Exception ex)
        {
            logger.LogError(ex, "Error in event handler!");
        }
    }

    private async Task SendNotificationAsync(string notificationXml, OAuthToken tokens, NotificationRegistration registration)
    {
        //logger.LogInformation("Posting {Xml} to {Url}", notificationXml, registration.ChannelUrl);

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
                return;

            logger.LogWarning("Failed to post notification! {StatusCode}", response.StatusCode);

            switch (response.StatusCode)
            {
                case HttpStatusCode.Unauthorized:
                    tokens = await GetAccessTokens(true);
                    await SendNotificationAsync(notificationXml, tokens, registration);
                    return;
                case HttpStatusCode.Gone:
                case HttpStatusCode.NotFound:
                    break; // TODO: get rid of it
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
    }

    private async Task<List<NotificationRegistration>> GetRegistrationsAsync(NotificationEvent notificationEvent)
    {
        await using var scope = services.CreateAsyncScope();
        await using var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

        var subject = notificationEvent.SubjectDid.ToString();
        var registrations = await db.Registrations.Where(r => r.Did == subject)
            .ToListAsync();

        return registrations;
    }

    private OAuthToken cache;
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

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        WeakReferenceMessenger.Default.Register(this);

        await GetAccessTokens();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        WeakReferenceMessenger.Default.Unregister<NotificationEventMessage>(this);

        return Task.CompletedTask;
    }

    public void Receive(NotificationEventMessage message)
    {
        message.Reply(OnNotificationEvent(message.Event));
    }
}
