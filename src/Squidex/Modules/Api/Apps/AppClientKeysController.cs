﻿// ==========================================================================
//  AppClientKeysController.cs
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex Group
//  All rights reserved.
// ==========================================================================

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;
using Squidex.Infrastructure.CQRS.Commands;
using Squidex.Infrastructure.Reflection;
using Squidex.Modules.Api.Apps.Models;
using Squidex.Pipeline;
using Squidex.Read.Apps.Services;
using Squidex.Write.Apps;
using Squidex.Write.Apps.Commands;

namespace Squidex.Modules.Api.Apps
{
    [Authorize(Roles = "app-owner")]
    [ApiExceptionFilter]
    [ServiceFilter(typeof(AppFilterAttribute))]
    public class AppClientKeysController : ControllerBase
    {
        private readonly IAppProvider appProvider;
        private readonly ClientKeyGenerator keyGenerator;

        public AppClientKeysController(ICommandBus commandBus, IAppProvider appProvider, ClientKeyGenerator keyGenerator)
            : base(commandBus)
        {
            this.appProvider = appProvider;
            this.keyGenerator = keyGenerator;
        }

        /// <summary>
        /// Get app client keys.
        /// </summary>
        /// <param name="app">The name of the app.</param>
        /// <remarks>
        /// Gets all configured client keys for the app with the specified name.
        /// </remarks>
        [HttpGet]
        [Route("apps/{app}/client-keys/")]
        [SwaggerTags("Apps")]
        [DescribedResponseType(200, typeof(ClientKeyDto[]), "Client keys returned..")]
        public async Task<IActionResult> GetContributors(string app)
        {
            var entity = await appProvider.FindAppByNameAsync(app);

            if (entity == null)
            {
                return NotFound();
            }

            var model = entity.ClientKeys.Select(x => SimpleMapper.Map(x, new ClientKeyDto())).ToList();

            return Ok(model);
        }

        /// <summary>
        /// Create new client key.
        /// </summary>
        /// <param name="app">The name of the app.</param>
        /// <remarks>
        /// Create a new client key for the app with the specified name. 
        /// The client key is auto generated on the server and returned.
        /// </remarks>
        [HttpPost]
        [Route("apps/{app}/client-keys/")]
        [SwaggerTags("Apps")]
        [DescribedResponseType(201, typeof(ClientKeyCreatedDto[]), "Client key created.")]
        public async Task<IActionResult> PostClientKey(string app)
        {
            var clientKey = keyGenerator.GenerateKey();

            await CommandBus.PublishAsync(new CreateClientKey { ClientKey = clientKey });

            return StatusCode(201, new ClientKeyCreatedDto { ClientKey = clientKey });
        }
    }
}
