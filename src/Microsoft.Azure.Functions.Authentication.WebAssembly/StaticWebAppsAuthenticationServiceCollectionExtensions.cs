// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.Functions.Authentication.WebAssembly 
{
    public static class StaticWebAppsAuthenticationServiceCollectionExtensions
    {
        public static IServiceCollection AddStaticWebAppsAuthentication(this IServiceCollection services)
        {
            return services.AddStaticWebAppsAuthentication<RemoteAuthenticationState, RemoteUserAccount, AppServiceAuthOptions>();
        }

        public static IServiceCollection AddStaticWebAppsAuthentication<TRemoteAuthenticationState, TAccount, TProviderOptions>(this IServiceCollection services)
            where TRemoteAuthenticationState : RemoteAuthenticationState
            where TAccount : RemoteUserAccount
            where TProviderOptions : AppServiceAuthOptions, new()
        {
            services.AddRemoteAuthentication<TRemoteAuthenticationState, TAccount, TProviderOptions>();

            services.AddScoped<AuthenticationStateProvider, AppServiceAuthRemoteAuthenticationService<TRemoteAuthenticationState>>();

            services.AddSingleton<AppServiceAuthMemoryStorage>();

            return services;
        }
    }
}