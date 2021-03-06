﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Azure.AppService.Authentication.WebAssembly.Models;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Microsoft.Azure.AppService.Authentication.WebAssembly
{
    class AppServiceAuthRemoteAuthenticationService<TAuthenticationState> : AuthenticationStateProvider, IRemoteAuthenticationService<TAuthenticationState> where TAuthenticationState : RemoteAuthenticationState
    {
        const string browserStorageType = "sessionStorage";
        const string storageKeyPrefix = "Blazor.AppServiceAuth";
        readonly AppServiceAuthMemoryStorage memoryStorage;

        public RemoteAuthenticationOptions<AppServiceAuthOptions> Options { get; }
        public HttpClient HttpClient { get; }
        public NavigationManager Navigation { get; }
        public IJSRuntime JSRuntime { get; }

        public AppServiceAuthRemoteAuthenticationService(
            IOptions<RemoteAuthenticationOptions<AppServiceAuthOptions>> options,
            NavigationManager navigationManager,
            IJSRuntime jsRuntime,
            AppServiceAuthMemoryStorage memoryStorage)
        {
            this.Options = options.Value;
            this.HttpClient = new HttpClient() { BaseAddress = new Uri(navigationManager.BaseUri) };
            this.Navigation = navigationManager;
            this.JSRuntime = jsRuntime;
            this.memoryStorage = memoryStorage;
        }

        public async override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                if (this.memoryStorage.AuthenticationData == null)
                {
                    string authDataUrl = this.Options.ProviderOptions.AuthenticationDataUrl + "/.auth/me";
                    AuthenticationData data = await this.HttpClient.GetFromJsonAsync<AuthenticationData>(authDataUrl);
                    this.memoryStorage.SetAuthenticationData(data);
                }

                ClientPrincipal principal = this.memoryStorage.AuthenticationData.ClientPrincipal;

                if (principal == null)
                {
                    return new AuthenticationState(new ClaimsPrincipal());
                }

                principal.UserRoles = principal.UserRoles.Except(new string[] { "anonymous" }, StringComparer.CurrentCultureIgnoreCase);

                var identity = new ClaimsIdentity(principal.IdentityProvider);
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, principal.UserId));
                identity.AddClaim(new Claim(ClaimTypes.Name, principal.UserDetails));
                identity.AddClaims(principal.UserRoles.Select(r => new Claim(ClaimTypes.Role, r)));
                return new AuthenticationState(new ClaimsPrincipal(identity));
            }
            catch
            {
                this.memoryStorage.SetAuthenticationData(null);
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }
        }

        public async Task<RemoteAuthenticationResult<TAuthenticationState>> SignInAsync(RemoteAuthenticationContext<TAuthenticationState> context)
        {
            if (!(context is AppServiceAuthRemoteAuthenticationContext<TAuthenticationState> AppServiceAuthContext))
            {
                throw new InvalidOperationException("Not an AppServiceAuthContext");
            }

            string stateId = Guid.NewGuid().ToString();
            await this.JSRuntime.InvokeVoidAsync($"{browserStorageType}.setItem", $"{storageKeyPrefix}.{stateId}", JsonSerializer.Serialize(context.State));
            this.Navigation.NavigateTo($"/.auth/login/{AppServiceAuthContext.SelectedProvider}?post_login_redirect_uri={this.BuildRedirectUri(this.Options.AuthenticationPaths.LogInCallbackPath)}/{stateId}", forceLoad: true);

            return new RemoteAuthenticationResult<TAuthenticationState> { Status = RemoteAuthenticationStatus.Redirect };
        }

        public async Task<RemoteAuthenticationResult<TAuthenticationState>> CompleteSignInAsync(RemoteAuthenticationContext<TAuthenticationState> context)
        {
            string stateId = new Uri(context.Url).PathAndQuery.Split("?")[0].Split("/", StringSplitOptions.RemoveEmptyEntries).Last();
            string serializedState = await this.JSRuntime.InvokeAsync<string>($"{browserStorageType}.getItem", $"{storageKeyPrefix}.{stateId}");
            TAuthenticationState state = JsonSerializer.Deserialize<TAuthenticationState>(serializedState);
            return new RemoteAuthenticationResult<TAuthenticationState> { State = state, Status = RemoteAuthenticationStatus.Success };
        }

        public async Task<RemoteAuthenticationResult<TAuthenticationState>> CompleteSignOutAsync(RemoteAuthenticationContext<TAuthenticationState> context)
        {
            string[] sessionKeys = await this.JSRuntime.InvokeAsync<string[]>("eval", $"Object.keys({browserStorageType})");

            string stateKey = sessionKeys.FirstOrDefault(key => key.StartsWith(storageKeyPrefix));

            if (stateKey != null)
            {
                await this.JSRuntime.InvokeAsync<string>($"{browserStorageType}.removeItem", stateKey);
            }

            return new RemoteAuthenticationResult<TAuthenticationState> { Status = RemoteAuthenticationStatus.Success };
        }

        public Task<RemoteAuthenticationResult<TAuthenticationState>> SignOutAsync(RemoteAuthenticationContext<TAuthenticationState> context)
        {
            this.Navigation.NavigateTo($"/.auth/logout?post_logout_redirect_uri={this.BuildRedirectUri(this.Options.AuthenticationPaths.LogOutCallbackPath)}", forceLoad: true);

            return Task.FromResult(new RemoteAuthenticationResult<TAuthenticationState> { Status = RemoteAuthenticationStatus.Redirect });
        }

        string BuildRedirectUri(string path)
        {
            return new Uri(new Uri(this.Navigation.BaseUri), path).ToString();
        }
    }
}
