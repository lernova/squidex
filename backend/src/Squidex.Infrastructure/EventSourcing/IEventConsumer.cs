﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Events;

namespace Squidex.Infrastructure.EventSourcing;

public interface IEventConsumer
{
    int BatchDelay => 500;

    int BatchSize => 1;

    string Name => GetType().Name;

    StreamFilter EventsFilter => default;

    bool StartLatest => false;

    bool CanClear => true;

    ValueTask<bool> HandlesAsync(StoredEvent @event)
    {
        return new ValueTask<bool>(true);
    }

    Task ClearAsync()
    {
        return Task.CompletedTask;
    }

    Task On(Envelope<IEvent> @event)
    {
        return Task.CompletedTask;
    }

    async Task On(IEnumerable<Envelope<IEvent>> events)
    {
        foreach (var @event in events)
        {
            await On(@event);
        }
    }
}
