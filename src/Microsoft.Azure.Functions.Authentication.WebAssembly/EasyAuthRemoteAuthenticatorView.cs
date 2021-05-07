// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.JSInterop;

namespace Microsoft.Azure.Functions.Authentication.WebAssembly 
{
    public class EasyAuthRemoteAuthenticatorView : EasyAuthRemoteAuthenticatorViewCore<RemoteAuthenticationState>
    {
        public EasyAuthRemoteAuthenticatorView() => this.AuthenticationState = new RemoteAuthenticationState();
    }

    public class EasyAuthRemoteAuthenticatorViewCore<TAuthenticationState> : RemoteAuthenticatorViewCore<TAuthenticationState> where TAuthenticationState : RemoteAuthenticationState
    {
        string message;

        [Parameter] public string SelectedOption { get; set; }

        [Inject] NavigationManager Navigation { get; set; }

        [Inject] IJSRuntime JS { get; set; }

        [Inject] IRemoteAuthenticationService<TAuthenticationState> AuthenticationService { get; set; }

        protected async override Task OnParametersSetAsync()
        {
            switch (this.Action)
            {
                case RemoteAuthenticationActions.LogIn:
                    if (this.SelectedOption != null)
                    {
                        await this.ProcessLogin(this.GetReturnUrl(state: null));
                    }
                    return;

                // Doing this because the SignOutManager intercepts the call otherwise and it'll fail
                // TODO: Investigate a custom SignOutManager
                case RemoteAuthenticationActions.LogOut:
                    RemoteAuthenticationResult<TAuthenticationState> result = await this.AuthenticationService.SignOutAsync(new EasyAuthRemoteAuthenticationContext<TAuthenticationState> { State = AuthenticationState });
                    switch (result.Status)
                    {
                        case RemoteAuthenticationStatus.Redirect:
                            break;
                        case RemoteAuthenticationStatus.Success:
                            await this.OnLogOutSucceeded.InvokeAsync(result.State);
                            await this.NavigateToReturnUrl(this.GetReturnUrl(this.AuthenticationState));
                            break;
                        case RemoteAuthenticationStatus.OperationCompleted:
                            break;
                        case RemoteAuthenticationStatus.Failure:
                            this.Navigation.NavigateTo(this.ApplicationPaths.LogOutFailedPath);
                            break;
                        default:
                            throw new InvalidOperationException($"Invalid authentication result status.");
                    }

                    break;

                default:
                    await base.OnParametersSetAsync();
                    break;
            }
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            switch (this.Action)
            {
                case RemoteAuthenticationActions.LogInFailed:
                    builder.AddContent(0, this.LogInFailed(this.message));
                    break;
                case RemoteAuthenticationActions.LogOutFailed:
                    builder.AddContent(0, this.LogOutFailed(this.message));
                    break;
                default:
                    base.BuildRenderTree(builder);
                    break;
            }
        }

        async Task ProcessLogin(string returnUrl)
        {
            this.AuthenticationState.ReturnUrl = returnUrl;
            RemoteAuthenticationResult<TAuthenticationState> result =
                await this.AuthenticationService.SignInAsync(new EasyAuthRemoteAuthenticationContext<TAuthenticationState>
                {
                    State = AuthenticationState,
                    SelectedProvider = SelectedOption
                });

            switch (result.Status)
            {
                case RemoteAuthenticationStatus.Redirect:
                    break;
                case RemoteAuthenticationStatus.Success:
                    await this.OnLogInSucceeded.InvokeAsync(result.State);
                    await this.NavigateToReturnUrl(this.GetReturnUrl(result.State, returnUrl));
                    break;
                case RemoteAuthenticationStatus.Failure:
                    this.message = result.ErrorMessage;
                    this.Navigation.NavigateTo(this.ApplicationPaths.LogInFailedPath);
                    break;
                case RemoteAuthenticationStatus.OperationCompleted:
                    break;
                default:
                    break;
            }
        }

        ValueTask NavigateToReturnUrl(string returnUrl)
        {
            return this.JS.InvokeVoidAsync("Blazor.navigateTo", returnUrl, false, true);
        }

        string GetReturnUrl(RemoteAuthenticationState state, string defaultReturnUrl = null)
        {
            if (state?.ReturnUrl != null)
            {
                return state.ReturnUrl;
            }

            string fromQuery = GetParameter(new Uri(this.Navigation.Uri).Query, "returnUrl");
            if (!string.IsNullOrWhiteSpace(fromQuery) && !fromQuery.StartsWith(this.Navigation.BaseUri))
            {
                // This is an extra check to prevent open redirects.
                throw new InvalidOperationException("Invalid return url. The return url needs to have the same origin as the current page.");
            }

            return fromQuery ?? defaultReturnUrl ?? this.Navigation.BaseUri;
        }

        internal static string GetParameter(string queryString, string key)
        {
            if (string.IsNullOrEmpty(queryString) || queryString == "?")
            {
                return null;
            }

            int scanIndex = 0;
            if (queryString[0] == '?')
            {
                scanIndex = 1;
            }

            int textLength = queryString.Length;
            int equalIndex = queryString.IndexOf('=');
            if (equalIndex == -1)
            {
                equalIndex = textLength;
            }

            while (scanIndex < textLength)
            {
                int ampersandIndex = queryString.IndexOf('&', scanIndex);
                if (ampersandIndex == -1)
                {
                    ampersandIndex = textLength;
                }

                if (equalIndex < ampersandIndex)
                {
                    while (scanIndex != equalIndex && char.IsWhiteSpace(queryString[scanIndex]))
                    {
                        ++scanIndex;
                    }

                    string name = queryString[scanIndex..equalIndex];
                    string value = queryString.Substring(equalIndex + 1, ampersandIndex - equalIndex - 1);
                    string processedName = Uri.UnescapeDataString(name.Replace('+', ' '));
                    if (string.Equals(processedName, key, StringComparison.OrdinalIgnoreCase))
                    {
                        return Uri.UnescapeDataString(value.Replace('+', ' '));
                    }

                    equalIndex = queryString.IndexOf('=', ampersandIndex);
                    if (equalIndex == -1)
                    {
                        equalIndex = textLength;
                    }
                }
                else
                {
                    if (ampersandIndex > scanIndex)
                    {
                        string value = queryString[scanIndex..ampersandIndex];
                        if (string.Equals(value, key, StringComparison.OrdinalIgnoreCase))
                        {
                            return string.Empty;
                        }
                    }
                }

                scanIndex = ampersandIndex + 1;
            }

            return null;
        }
    }

    public class EasyAuthRemoteAuthenticationContext<TAuthenticationState> : RemoteAuthenticationContext<TAuthenticationState> where TAuthenticationState : RemoteAuthenticationState
    {
        public string SelectedProvider { get; set; }
    }
}
