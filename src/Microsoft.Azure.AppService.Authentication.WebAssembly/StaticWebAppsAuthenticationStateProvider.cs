// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Azure.AppService.Authentication.WebAssembly.Models;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Azure.AppService.Authentication.WebAssembly
{
    public class StaticWebAppsAuthenticationStateProvider : AuthenticationStateProvider
    {
        readonly IConfiguration config;
        readonly HttpClient http;

        public StaticWebAppsAuthenticationStateProvider(IConfiguration config, IWebAssemblyHostEnvironment environment)
        {
            this.config = config;
            this.http = new HttpClient { BaseAddress = new Uri(environment.BaseAddress) };
        }

        public async override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                string authDataUrl = this.config.GetValue("StaticWebAppsAuthentication:AuthenticationDataUrl", "/.auth/me");
                string json = await this.http.GetStringAsync(authDataUrl);

                ClaimsPrincipal user = ParseClaims(json);
                return new AuthenticationState(user);
            }
            catch
            {
                return new AuthenticationState(new ClaimsPrincipal());
            }
        }

        public static ClaimsPrincipal ParseClaims(string json)
        {
            AuthenticationData data = JsonSerializer.Deserialize<AuthenticationData>(json);

            ClientPrincipal principal = data.ClientPrincipal;
            principal.UserRoles = principal.UserRoles.Except(new string[] { "anonymous" }, StringComparer.CurrentCultureIgnoreCase);

            if (!principal.UserRoles.Any())
            {
                return new ClaimsPrincipal();
            }

            var identity = new ClaimsIdentity(principal.IdentityProvider);
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, principal.UserId));
            identity.AddClaim(new Claim(ClaimTypes.Name, principal.UserDetails));
            identity.AddClaims(principal.UserRoles.Select(r => new Claim(ClaimTypes.Role, r)));
            return new ClaimsPrincipal(identity);
        }
    }
}