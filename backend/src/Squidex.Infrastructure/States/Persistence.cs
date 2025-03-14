﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Events;
using Squidex.Infrastructure.EventSourcing;

#pragma warning disable RECS0012 // 'if' statement can be re-written as 'switch' statement

namespace Squidex.Infrastructure.States;

internal sealed class Persistence<T>(
    DomainId ownerKey,
    Type ownerType,
    PersistenceMode persistenceMode,
    IEventFormatter eventFormatter,
    IEventStore eventStore,
    IEventStreamNames eventStreamNames,
    ISnapshotStore<T> snapshotStore,
    HandleSnapshot<T>? applyState,
    HandleEvent? applyEvent)
    : IPersistence<T>
{
    private readonly Lazy<string> streamName = new Lazy<string>(() => eventStreamNames.GetStreamName(ownerType, ownerKey.ToString()!));
    private long versionSnapshot = EtagVersion.Empty;
    private long versionEvents = EtagVersion.Empty;
    private long version = EtagVersion.Empty;

    public long Version
    {
        get => version;
    }

    private bool UseSnapshots
    {
        get => (persistenceMode & PersistenceMode.Snapshots) == PersistenceMode.Snapshots;
    }

    private bool UseEventSourcing
    {
        get => (persistenceMode & PersistenceMode.EventSourcing) == PersistenceMode.EventSourcing;
    }

    public bool IsSnapshotStale
    {
        get => UseSnapshots && UseEventSourcing && versionSnapshot < versionEvents;
    }

    public async Task DeleteAsync(
        CancellationToken ct = default)
    {
        if (UseSnapshots)
        {
            using (Telemetry.Activities.StartActivity("Persistence/ReadState"))
            {
                await snapshotStore.RemoveAsync(ownerKey, ct);
            }

            versionSnapshot = EtagVersion.Empty;
        }

        if (UseEventSourcing)
        {
            using (Telemetry.Activities.StartActivity("Persistence/ReadEvents"))
            {
                await eventStore.DeleteAsync(StreamFilter.Name(streamName.Value), ct);
            }

            versionEvents = EtagVersion.Empty;
        }

        UpdateVersion();
    }

    public async Task ReadAsync(long expectedVersion = EtagVersion.Any,
        CancellationToken ct = default)
    {
        versionSnapshot = EtagVersion.Empty;
        versionEvents = EtagVersion.Empty;

        if (UseSnapshots)
        {
            await ReadSnapshotAsync(ct);
        }

        if (UseEventSourcing)
        {
            await ReadEventsAsync(ct);
        }

        UpdateVersion();

        if (expectedVersion > EtagVersion.Any && expectedVersion != version)
        {
            if (version == EtagVersion.Empty)
            {
                throw new DomainObjectNotFoundException(ownerKey.ToString()!);
            }
            else
            {
                throw new InconsistentStateException(version, expectedVersion);
            }
        }
    }

    private async Task ReadSnapshotAsync(
        CancellationToken ct)
    {
        var (_, state, versionRead, valid) = await snapshotStore.ReadAsync(ownerKey, ct);

        versionRead = Math.Max(versionRead, EtagVersion.Empty);
        versionSnapshot = versionRead;

        if (valid)
        {
            versionEvents = versionRead;
        }

        if (applyState != null && versionRead > EtagVersion.Empty && valid)
        {
            applyState(state, versionRead);
        }
    }

    private async Task ReadEventsAsync(
        CancellationToken ct)
    {
        var events = await eventStore.QueryStreamAsync(streamName.Value, versionEvents, ct);

        var isStopped = false;

        foreach (var @event in events)
        {
            var newVersion = versionEvents + 1;

            if (@event.EventStreamNumber != newVersion)
            {
                ThrowHelper.InvalidOperationException("Events must follow the snapshot version in consecutive order with no gaps.");
            }

            if (!isStopped)
            {
                var parsedEvent = eventFormatter.ParseIfKnown(@event);

                if (applyEvent != null && parsedEvent != null)
                {
                    isStopped = !applyEvent(parsedEvent);
                }
            }

            versionEvents++;
        }
    }

    public async Task WriteSnapshotAsync(T state,
        CancellationToken ct = default)
    {
        var oldVersion = versionSnapshot;

        if (oldVersion == EtagVersion.Empty && UseEventSourcing)
        {
            oldVersion = versionEvents - 1;
        }

        var newVersion = UseEventSourcing ? versionEvents : oldVersion + 1;

        if (newVersion == versionSnapshot)
        {
            return;
        }

        using (Telemetry.Activities.StartActivity("Persistence/WriteState"))
        {
            var job = new SnapshotWriteJob<T>(ownerKey, state, newVersion)
            {
                OldVersion = oldVersion,
            };

            await snapshotStore.WriteAsync(job, ct);
        }

        versionSnapshot = newVersion;

        UpdateVersion();
    }

    public async Task WriteEventsAsync(IReadOnlyList<Envelope<IEvent>> events,
        CancellationToken ct = default)
    {
        Guard.NotEmpty(events);

        var oldVersion = EtagVersion.Any;

        if (UseEventSourcing)
        {
            oldVersion = versionEvents;
        }

        var eventCommitId = Guid.NewGuid();
        var eventData = events.Select(x => eventFormatter.ToEventData(x, eventCommitId, true)).ToArray();

        try
        {
            using (Telemetry.Activities.StartActivity("Persistence/WriteEvents"))
            {
                await eventStore.AppendAsync(eventCommitId, streamName.Value, oldVersion, eventData, ct);
            }
        }
        catch (WrongEventVersionException ex)
        {
            throw new InconsistentStateException(ex.VersionCurrent, ex.VersionExpected, ex);
        }

        versionEvents += eventData.Length;
    }

    private void UpdateVersion()
    {
        if (persistenceMode == PersistenceMode.Snapshots)
        {
            version = versionSnapshot;
        }
        else if (persistenceMode == PersistenceMode.EventSourcing)
        {
            version = versionEvents;
        }
        else if (persistenceMode == PersistenceMode.SnapshotsAndEventSourcing)
        {
            version = Math.Max(versionEvents, versionSnapshot);
        }
    }
}
