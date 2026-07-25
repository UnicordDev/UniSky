using System.Net.WebSockets;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using Polly;
using Polly.Retry;

namespace UniSky.Notifications.Services;

public sealed class SpacedustConnection : IAsyncDisposable
{
    private readonly string[] dids;
    private readonly Uri baseUri;
    private readonly ILogger logger;
    private readonly Func<JsonDocument, Task> onMessage;
    private readonly CancellationTokenSource cts = new();
    private readonly ResiliencePipeline reconnectPipeline;

    private Task? runTask;

    public IReadOnlyCollection<string> Dids => dids;

    public SpacedustConnection(
        IReadOnlyCollection<string> dids,
        Uri baseUri,
        ILogger logger,
        Func<JsonDocument, Task> onMessage)
    {
        this.dids = [.. dids];
        this.baseUri = baseUri;
        this.logger = logger;
        this.onMessage = onMessage;

        reconnectPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                // Retry every failure except cancellation, which means we're shutting down / rebalancing.
                ShouldHandle = args => ValueTask.FromResult(
                    args.Outcome.Exception is not null and not OperationCanceledException),
                MaxRetryAttempts = int.MaxValue,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                MaxDelay = TimeSpan.FromMinutes(2),
                OnRetry = args =>
                {
                    logger.LogWarning(args.Outcome.Exception,
                        "Reconnecting Spacedust shard ({Dids} DIDs), attempt {N}...",
                        this.dids.Length, args.AttemptNumber);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    public void Start()
    {
        runTask = RunWithReconnectAsync();
    }

    private async Task RunWithReconnectAsync()
    {
        try
        {
            await reconnectPipeline.ExecuteAsync(async token => await RunAsync(token), cts.Token);
        }
        catch (OperationCanceledException)
        {
            // expected on shutdown / rebalance
        }
    }

    private async Task RunAsync(CancellationToken token)
    {
        var parameters = new List<KeyValuePair<string, StringValues>>
        {
            new("wantedSubjectDids", new StringValues(dids)),
            new("instant", new StringValues("true"))
        };

        var uri = new Uri(QueryHelpers.AddQueryString(baseUri.ToString(), parameters));
        var socket = new ClientWebSocket();
        socket.Options.KeepAliveInterval = TimeSpan.Zero;
        await socket.ConnectAsync(uri, token);

        logger.LogInformation("Connected to Spacedust! Listening for {Dids} DIDs.", dids.Length);

        try
        {
            await MessageLoop(socket, token);
        }
        finally
        {
            if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
        }
    }

    private async Task MessageLoop(WebSocket socket, CancellationToken token)
    {
        var jsonState = new JsonReaderState();
        var buffer = WebSocket.CreateClientBuffer(16 * 1024, 16 * 1024);

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
                        _ = Task.Run(() => onMessage(doc), token);
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

    public async ValueTask DisposeAsync()
    {
        try
        {
            await cts.CancelAsync();

            if (runTask != null)
                await runTask;
        }
        catch (OperationCanceledException)
        {
            // expected on shutdown
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Error while disposing Spacedust shard.");
        }
        finally
        {
            cts.Dispose();
        }
    }
}
