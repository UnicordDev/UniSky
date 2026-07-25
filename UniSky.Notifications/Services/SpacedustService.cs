using System.Globalization;
using System.Text.Json;
using CommunityToolkit.Mvvm.Messaging;
using FishyFlip.Models;
using UniSky.Notifications.Data;
using UniSky.Notifications.Messages;
using UniSky.Notifications.Models;

namespace UniSky.Notifications.Services;

public class SpacedustService(
    ILogger<SpacedustService> logger,
    ILoggerFactory loggerFactory,
    IConfiguration configuration,
    IServiceProvider services) : IHostedService, IRecipient<RegistrationsUpdatedMessage>
{
    private const int DefaultMaxDidsPerConnection = 100;
    private const string DefaultSpacedustUri = "wss://spacedust.microcosm.blue/subscribe";
    private const int CompactionSlack = 2;

    private sealed class Bucket
    {
        public HashSet<string> Dids { get; } = [];
        public SpacedustConnection? Connection { get; set; }
    }

    private readonly SemaphoreSlim reconcileLock = new(1, 1);
    private readonly List<Bucket> buckets = [];
    private readonly Dictionary<string, Bucket> didToBucket = [];

    private int MaxDidsPerConnection =>
        Math.Max(1, configuration.GetValue("Spacedust:MaxDidsPerConnection", DefaultMaxDidsPerConnection));

    private Uri SpacedustUri =>
        new(configuration.GetValue("Spacedust:Uri", DefaultSpacedustUri)!);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await ReconcileAsync();

        WeakReferenceMessenger.Default.Register<RegistrationsUpdatedMessage>(this);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        WeakReferenceMessenger.Default.Unregister<RegistrationsUpdatedMessage>(this);

        await reconcileLock.WaitAsync(cancellationToken);
        try
        {
            foreach (var bucket in buckets)
            {
                if (bucket.Connection != null)
                    await bucket.Connection.DisposeAsync();
            }

            buckets.Clear();
            didToBucket.Clear();
        }
        finally
        {
            reconcileLock.Release();
        }
    }

    private async Task ReconcileAsync()
    {
        await reconcileLock.WaitAsync();
        try
        {
            var wantedDids = await LoadWantedDidsAsync();

            var removed = didToBucket.Keys.Where(did => !wantedDids.Contains(did)).ToArray();
            var added = wantedDids.Where(did => !didToBucket.ContainsKey(did)).ToArray();

            if (removed.Length == 0 && added.Length == 0)
            {
                logger.LogInformation("No registration changes. {Count} shard(s) unchanged.", buckets.Count);
                return;
            }

            var dirty = new HashSet<Bucket>();

            foreach (var did in removed)
            {
                if (didToBucket.Remove(did, out var bucket))
                {
                    bucket.Dids.Remove(did);
                    dirty.Add(bucket);
                }
            }

            var maxPerConnection = MaxDidsPerConnection;
            foreach (var did in added)
                dirty.Add(PlaceDid(did, maxPerConnection));

            // Fragmentation from removals can leave many sparse buckets => more sockets than necessary.
            // When that drift gets large, do a full re-pack instead of touching only the dirty buckets.
            var minBuckets = (wantedDids.Count + maxPerConnection - 1) / maxPerConnection;
            var nonEmptyBuckets = buckets.Count(b => b.Dids.Count > 0);
            if (nonEmptyBuckets > minBuckets + CompactionSlack)
            {
                logger.LogInformation(
                    "Compacting Spacedust shards ({Current} => {Target}).", nonEmptyBuckets, minBuckets);
                await RebuildAllAsync(wantedDids);
                return;
            }

            foreach (var bucket in dirty)
                await ReconnectBucketAsync(bucket);

            buckets.RemoveAll(b => b.Dids.Count == 0);

            logger.LogInformation(
                "Reconciled Spacedust shards: +{Added} -{Removed} DIDs across {Count} shard(s).",
                added.Length, removed.Length, buckets.Count);
        }
        finally
        {
            reconcileLock.Release();
        }
    }

    private async Task AddDidAsync(string did)
    {
        await reconcileLock.WaitAsync();
        try
        {
            if (didToBucket.ContainsKey(did))
                return;

            var bucket = PlaceDid(did, MaxDidsPerConnection);
            await ReconnectBucketAsync(bucket);

            logger.LogInformation("Added DID to Spacedust; now {Count} shard(s).", buckets.Count);
        }
        finally
        {
            reconcileLock.Release();
        }
    }

    private Bucket PlaceDid(string did, int maxPerConnection)
    {
        var bucket = buckets.FirstOrDefault(b => b.Dids.Count < maxPerConnection);
        if (bucket == null)
        {
            bucket = new Bucket();
            buckets.Add(bucket);
        }

        bucket.Dids.Add(did);
        didToBucket[did] = bucket;
        return bucket;
    }

    private async Task<HashSet<string>> LoadWantedDidsAsync()
    {
        var wantedDids = new HashSet<string>();

        await using var scope = services.CreateAsyncScope();
        await using var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

        await foreach (var registration in db.Registrations)
            wantedDids.Add(registration.Did);

        return wantedDids;
    }

    private async Task ReconnectBucketAsync(Bucket bucket)
    {
        if (bucket.Connection != null)
        {
            await bucket.Connection.DisposeAsync();
            bucket.Connection = null;
        }

        if (bucket.Dids.Count == 0)
            return;

        var connection = new SpacedustConnection(
            bucket.Dids,
            SpacedustUri,
            loggerFactory.CreateLogger<SpacedustConnection>(),
            HandleMessage);
        connection.Start();
        bucket.Connection = connection;
    }

    private async Task RebuildAllAsync(HashSet<string> wantedDids)
    {
        foreach (var bucket in buckets)
        {
            if (bucket.Connection != null)
                await bucket.Connection.DisposeAsync();
        }

        buckets.Clear();
        didToBucket.Clear();

        if (wantedDids.Count == 0)
        {
            logger.LogInformation("No DIDs to listen for. Not connecting.");
            return;
        }

        var maxPerConnection = MaxDidsPerConnection;
        Bucket? current = null;
        foreach (var did in wantedDids)
        {
            if (current == null || current.Dids.Count >= maxPerConnection)
            {
                current = new Bucket();
                buckets.Add(current);
            }

            current.Dids.Add(did);
            didToBucket[did] = current;
        }

        foreach (var bucket in buckets)
            await ReconnectBucketAsync(bucket);

        logger.LogInformation(
            "Connected to Spacedust across {Count} shard(s) for {Dids} DIDs.", buckets.Count, wantedDids.Count);
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
            subjectRecord!,
            null!);

        var message = new NotificationEventMessage(notificationEvent);

        await Task.WhenAll(WeakReferenceMessenger.Default.Send(message));
    }

    public void Receive(RegistrationsUpdatedMessage message)
    {
        message.Reply(message.Did is string did ? AddDidAsync(did) : ReconcileAsync());
    }
}
