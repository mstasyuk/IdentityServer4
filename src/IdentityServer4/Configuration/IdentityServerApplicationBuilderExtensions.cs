﻿// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4.Hosting;
using IdentityServer4.Stores;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Pipeline extension methods for adding IdentityServer
    /// </summary>
    public static class IdentityServerApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds IdentityServer to the pipeline.
        /// </summary>
        /// <param name="app">The application.</param>
        /// <returns></returns>
        public static IApplicationBuilder UseIdentityServer(this IApplicationBuilder app)
        {
            app.Validate();

            app.UseMiddleware<BaseUrlMiddleware>();

            // todo: review
            app.ConfigureCors();

            // it seems ok if we have UseAuthentication more than once in the pipeline --
            // this will just re-run the various callback handlers and the default authN 
            // handler, which just re-assigns the user on the context. claims transformation
            // will run twice, since that's not cached (whereas the authN handler result is)
            // todo: maybe we don't do this at all? maybe we become an authN MW?
            // we could handle our protocol endpoints with IAuthenticationHandlerProvider
            // related: https://github.com/aspnet/Security/issues/1399
            app.UseAuthentication();

            // todo: this needs to be moved in front of UseAuthentication to handle the new callback
            // mechanism for front-channel signout
            // todo: consider a decorator on the authN service to handle this instead of requiring 
            // config for all the signout callback paths. this would require dependencies on ws-fed and oidc MWs
            app.UseMiddleware<FederatedSignOutMiddleware>();

            app.UseMiddleware<IdentityServerMiddleware>();

            return app;
        }

        internal static void Validate(this IApplicationBuilder app)
        {
            var loggerFactory = app.ApplicationServices.GetService(typeof(ILoggerFactory)) as ILoggerFactory;
            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));

            var logger = loggerFactory.CreateLogger("IdentityServer4.Startup");

            var scopeFactory = app.ApplicationServices.GetService<IServiceScopeFactory>();

            using (var scope = scopeFactory.CreateScope())
            {
                var serviceProvider = scope.ServiceProvider;

                TestService(serviceProvider, typeof(IPersistedGrantStore), logger, "No storage mechanism for grants specified. Use the 'AddInMemoryPersistedGrants' extension method to register a development version.");
                TestService(serviceProvider, typeof(IClientStore), logger, "No storage mechanism for clients specified. Use the 'AddInMemoryClients' extension method to register a development version.");
                TestService(serviceProvider, typeof(IResourceStore), logger, "No storage mechanism for resources specified. Use the 'AddInMemoryIdentityResources' or 'AddInMemoryApiResources' extension method to register a development version.");

                var persistedGrants = serviceProvider.GetService(typeof(IPersistedGrantStore));
                if (persistedGrants.GetType().FullName == typeof(InMemoryPersistedGrantStore).FullName)
                {
                    logger.LogInformation("You are using the in-memory version of the persisted grant store. This will store consent decisions, authorization codes, refresh and reference tokens in memory only. If you are using any of those features in production, you want to switch to a different store implementation.");
                }

                // todo: cookie diagnostics
                //var logger = app.ApplicationServices.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(CookieConfiguration).FullName);
                //var schemes = app.ApplicationServices.GetRequiredService<IAuthenticationSchemeProvider>();

                //var defaultScheme = schemes.GetDefaultSignInSchemeAsync();
                //logger.LogDebug("Using {scheme} for sign-in", defaultScheme.)
            }
        }

        internal static object TestService(IServiceProvider serviceProvider, Type service, ILogger logger, string message = null, bool doThrow = true)
        {
            var appService = serviceProvider.GetService(service);

            if (appService == null)
            {
                var error = message ?? $"Required service {service.FullName} is not registered in the DI container. Aborting startup";

                logger.LogCritical(error);

                if (doThrow)
                {
                    throw new InvalidOperationException(error);
                }
            }

            return appService;
        }
    }
}
