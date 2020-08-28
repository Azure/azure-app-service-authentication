using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Authentication.WebAssembly.AppService
{
    public static class StaticWebAppsAuthenticationServiceCollectionExtensions
    {
        public static IServiceCollection AddStaticWebAppsAuthentication(this IServiceCollection services)
        {
            return services.AddStaticWebAppsAuthentication<RemoteAuthenticationState, RemoteUserAccount, EasyAuthOptions>();
        }
        public static IServiceCollection AddStaticWebAppsAuthentication<TRemoteAuthenticationState, TAccount, TProviderOptions>(this IServiceCollection services)
            where TRemoteAuthenticationState : RemoteAuthenticationState
            where TAccount : RemoteUserAccount
            where TProviderOptions : EasyAuthOptions, new()
        {
            services.AddRemoteAuthentication<TRemoteAuthenticationState, TAccount, TProviderOptions>();

            services.AddScoped<AuthenticationStateProvider, EasyAuthRemoteAuthenticationService>();

            return services;
        }
    }
}