﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Logging;
using NodaTime;
using Squidex.Domain.Apps.Core.Contents;
using Squidex.Domain.Apps.Entities.Contents.Commands;
using Squidex.Domain.Apps.Entities.Contents.Repositories;
using Squidex.Hosting;
using Squidex.Infrastructure;
using Squidex.Infrastructure.Commands;
using Squidex.Infrastructure.Timers;

namespace Squidex.Domain.Apps.Entities.Contents;

public sealed class ContentSchedulerProcess(
    IContentRepository contentRepository,
    ICommandBus commandBus,
    ILogger<ContentSchedulerProcess> log)
    : IBackgroundProcess
{
    private CompletionTimer timer;

    public IClock Clock { get; set; } = SystemClock.Instance;

    public Task StartAsync(
        CancellationToken ct)
    {
        timer = new CompletionTimer((int)TimeSpan.FromSeconds(10).TotalMilliseconds, PublishAsync);

        return Task.CompletedTask;
    }

    public Task StopAsync(
        CancellationToken ct)
    {
        return timer?.StopAsync() ?? Task.CompletedTask;
    }

    public async Task PublishAsync(
        CancellationToken ct = default)
    {
        try
        {
            var now = Clock.GetCurrentInstant();

            await foreach (var content in contentRepository.StreamScheduledWithoutDataAsync(now, SearchScope.All, ct))
            {
                await TryPublishAsync(content);
            }
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to query scheduled status changes-");
        }
    }

    private async Task TryPublishAsync(Content content)
    {
        var id = content.Id;

        try
        {
            var job = content.ScheduleJob;

            if (job != null)
            {
                var command = new ChangeContentStatus
                {
                    Actor = job.ScheduledBy,
                    AppId = content.AppId,
                    ContentId = id,
                    SchemaId = content.SchemaId,
                    Status = job.Status,
                    StatusJobId = job.Id,
                };

                await commandBus.PublishAsync(command, default);
            }
        }
        catch (DomainObjectNotFoundException)
        {
            await contentRepository.ResetScheduledAsync(content.AppId.Id, id, default);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to execute scheduled status change for content '{contentId}'.", content.Id);
        }
    }
}
