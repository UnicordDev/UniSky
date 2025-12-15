using System.Globalization;
using System.Net.WebSockets;
using System.Text.Json;
using CommunityToolkit.Mvvm.Messaging;
using FishyFlip.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using UniSky.Notifications.Data;
using UniSky.Notifications.Messages;
using UniSky.Notifications.Models;

namespace UniSky.Notifications.Services;

public class SpacedustService(
    ILogger<SpacedustService> logger,
    IServiceProvider services) : IHostedService, IRecipient<RegistrationsUpdatedMessage>
{
    private CancellationTokenSource? cts;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await ConnectAsync(cancellationToken);

        WeakReferenceMessenger.Default.Register<RegistrationsUpdatedMessage>(this);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (cts != null)
            await cts.CancelAsync();

        WeakReferenceMessenger.Default.Unregister<RegistrationsUpdatedMessage>(this);
    }

    private async Task ConnectAsync(CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        await using var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

        if (cts != null)
            await cts.CancelAsync();

        cts = new CancellationTokenSource();
        var wantedDids = new HashSet<string>();
        await foreach (var registation in db.Registrations)
        {
            wantedDids.Add(registation.Did);
        }

        if (wantedDids.Count == 0)
        {
            logger.LogInformation("No DIDs to listen for. Not connecting.");
            return;
        }

        var parameters = new List<KeyValuePair<string, StringValues>>
        {
            new("wantedSubjectDids", new StringValues([.. wantedDids])),
            new("instant", new StringValues("true"))
        };

        // TODO: configurable uri
        var uri = new Uri(QueryHelpers.AddQueryString("wss://spacedust.microcosm.blue/subscribe", parameters));
        var socket = new ClientWebSocket();
        socket.Options.KeepAliveInterval = TimeSpan.Zero;
        await socket.ConnectAsync(uri, cancellationToken);

        logger.LogInformation("Connected to Spacedust! Listening for {Dids} DIDs.", wantedDids.Count);

        _ = Task.Run(() => MessageLoop(socket, cts.Token));
    }

    private async Task MessageLoop(WebSocket socket, CancellationToken token = default)
    {
        var jsonState = new JsonReaderState();
        var buffer = WebSocket.CreateClientBuffer(16 * 1024, 16 * 1024);
        try
        {
            while (!token.IsCancellationRequested)
            {
                var result = await socket.ReceiveAsync(buffer, token);

                try
                {

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        throw new Exception($"Socket closed. {result.CloseStatus} {result.CloseStatusDescription}");
                    }

                    var jsonReader = new Utf8JsonReader(
                        buffer.Slice(0, result.Count),
                        isFinalBlock: result.EndOfMessage,
                        jsonState);

                    while (jsonReader.Read())
                    {
                        if (jsonReader.TokenType == JsonTokenType.StartObject)
                        {
                            var doc = JsonDocument.ParseValue(ref jsonReader);
                            _ = Task.Run(() => HandleMessage(doc));
                        }
                    }

                    jsonState = jsonReader.CurrentState;
                    if (result.EndOfMessage)
                        jsonState = new JsonReaderState();
                }
                catch (JsonException e)
                {
                    logger.LogError(e, "JSON parsing failed!");
                    throw;
                }
            }
        }
        catch (TaskCanceledException e)
        {
            logger.LogInformation(e, "Spacedust loop canceled");
        }
        catch (Exception e)
        {
            logger.LogError(e, "Spacedust loop failed, reconnecting...");

            _ = Task.Run(() => ConnectAsync(CancellationToken.None));
        }
        finally
        {
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
        }
    }

    private async Task HandleMessage(JsonDocument document)
    {
        using var doc = document;

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Got JSON document {Doc}", doc.RootElement);

        var spacedustEvent = JsonSerializer.Deserialize(document, AppJsonSerializerContext.Default.SpacedustEvent);
        if (spacedustEvent == null)
            return;

        ATDid subjectDid;
        ATUri? subjectRecord = null;
        var eventLink = spacedustEvent.Link;
        if (eventLink.Subject.StartsWith("at://"))
        {
            if (!ATUri.TryParse(eventLink.Subject, CultureInfo.InvariantCulture, out subjectRecord))
            {
                logger.LogWarning("Invalid ATUri in event. {Uri}", eventLink.Subject);
                return;
            }

            subjectDid = subjectRecord.Did!;
        }
        else if (!ATDid.TryParse(eventLink.Subject, CultureInfo.InvariantCulture, out subjectDid))
        {
            logger.LogWarning("Invalid ATDid in event. {Did}", eventLink.Subject);
            return;
        }

        if (subjectDid != null && subjectRecord == null)
            subjectRecord = new ATUri("at://" + subjectDid);

        if (!ATUri.TryParse(eventLink.SourceRecord, CultureInfo.InvariantCulture, out var sourceRecord))
        {
            logger.LogWarning("Invalid source_record in event. {Did}", eventLink.SourceRecord);
            return;
        }

        var notificationEvent = new NotificationEvent(
            spacedustEvent.Kind,
            eventLink.Source,
            sourceRecord.Collection,
            sourceRecord.Did!,
            sourceRecord,
            subjectDid!,
            subjectRecord!);

        var message = new NotificationEventMessage(notificationEvent);
        await Task.WhenAll(WeakReferenceMessenger.Default.Send(message));
    }

    public void Receive(RegistrationsUpdatedMessage message)
    {
        message.Reply(ConnectAsync(CancellationToken.None));
    }
}
