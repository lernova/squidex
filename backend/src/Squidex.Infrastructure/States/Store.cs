﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Events;
using Squidex.Infrastructure.EventSourcing;

namespace Squidex.Infrastructure.States;

public sealed class Store<T>(
    IEventFormatter eventFormatter,
    IEventStore eventStore,
    IEventStreamNames eventStreamNames,
    ISnapshotStore<T> snapshotStore)
    : IStore<T>
{
    public ISnapshotStore<T> Snapshots => snapshotStore;

    public Task ClearSnapshotsAsync()
    {
        return snapshotStore.ClearAsync();
    }

    public IBatchContext<T> WithBatchContext(Type owner)
    {
        return new BatchContext<T>(owner,
            eventFormatter,
            eventStore,
            eventStreamNames,
            snapshotStore);
    }

    public IPersistence<T> WithEventSourcing(Type owner, DomainId key, HandleEvent? applyEvent)
    {
        return CreatePersistence(owner, key, PersistenceMode.EventSourcing, null, applyEvent);
    }

    public IPersistence<T> WithSnapshots(Type owner, DomainId key, HandleSnapshot<T>? applySnapshot)
    {
        return CreatePersistence(owner, key, PersistenceMode.Snapshots, applySnapshot, null);
    }

    public IPersistence<T> WithSnapshotsAndEventSourcing(Type owner, DomainId key, HandleSnapshot<T>? applySnapshot, HandleEvent? applyEvent)
    {
        return CreatePersistence(owner, key, PersistenceMode.SnapshotsAndEventSourcing, applySnapshot, applyEvent);
    }

    private IPersistence<T> CreatePersistence(Type owner, DomainId key, PersistenceMode mode, HandleSnapshot<T>? applySnapshot, HandleEvent? applyEvent)
    {
        Guard.NotNull(key);

        return new Persistence<T>(key, owner,
            mode,
            eventFormatter,
            eventStore,
            eventStreamNames,
            snapshotStore,
            applySnapshot,
            applyEvent);
    }
}
