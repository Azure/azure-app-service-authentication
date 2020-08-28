using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace Microsoft.Authentication.WebAssembly.AppService
{
    public class EasyAuthRemoteAuthenticatorView : RemoteAuthenticatorView
    {
        private string _message;

        [Parameter] public string SelectedOption { get; set; }

        [Inject] NavigationManager Navigation { get; set; }

        [Inject] IJSRuntime JS { get; set; }

        [Inject] IRemoteAuthenticationService<RemoteAuthenticationState> AuthenticationService { get; set; }

        protected async override Task OnParametersSetAsync()
        {
            switch (Action)
            {
                case RemoteAuthenticationActions.LogIn:
                    if (SelectedOption != null)
                    {
                        await ProcessLogin(GetReturnUrl(state: null));
                    }
                    return;

                // Doing this because the SignOutManager intercepts the call otherwise and it'll fail
                // TODO: Investigate a custom SignOutManager
                case RemoteAuthenticationActions.LogOut:
                    await AuthenticationService.SignOutAsync(new EasyAuthRemoteAuthenticationContext
                    {
                        State = AuthenticationState
                    });
                    return;

                default:
                    await base.OnParametersSetAsync();
                    break;
            }
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            switch (Action)
            {
                case RemoteAuthenticationActions.LogInFailed:
                    builder.AddContent(0, LogInFailed(_message));
                    break;
                case RemoteAuthenticationActions.LogOutFailed:
                    builder.AddContent(0, LogOutFailed(_message));
                    break;
                default:
                    base.BuildRenderTree(builder);
                    break;
            }
        }

        private async Task ProcessLogin(string returnUrl)
        {
            AuthenticationState.ReturnUrl = returnUrl;
            var result = await AuthenticationService.SignInAsync(new EasyAuthRemoteAuthenticationContext
            {
                State = AuthenticationState,
                SelectedProvider = SelectedOption
            });

            switch (result.Status)
            {
                case RemoteAuthenticationStatus.Redirect:
                    break;
                case RemoteAuthenticationStatus.Success:
                    await OnLogInSucceeded.InvokeAsync(result.State);
                    await NavigateToReturnUrl(GetReturnUrl(result.State, returnUrl));
                    break;
                case RemoteAuthenticationStatus.Failure:
                    _message = result.ErrorMessage;
                    Navigation.NavigateTo(ApplicationPaths.LogInFailedPath);
                    break;
                case RemoteAuthenticationStatus.OperationCompleted:
                    break;
                default:
                    break;
            }
        }

        private async Task NavigateToReturnUrl(string returnUrl) => await JS.InvokeVoidAsync("Blazor.navigateTo", returnUrl, false, true);

        private string GetReturnUrl(RemoteAuthenticationState state, string defaultReturnUrl = null)
        {
            if (state?.ReturnUrl != null)
            {
                return state.ReturnUrl;
            }

            var fromQuery = GetParameter(new Uri(Navigation.Uri).Query, "returnUrl");
            if (!string.IsNullOrWhiteSpace(fromQuery) && !fromQuery.StartsWith(Navigation.BaseUri))
            {
                // This is an extra check to prevent open redirects.
                throw new InvalidOperationException("Invalid return url. The return url needs to have the same origin as the current page.");
            }

            return fromQuery ?? defaultReturnUrl ?? Navigation.BaseUri;
        }

        internal static string GetParameter(string queryString, string key)
        {
            if (string.IsNullOrEmpty(queryString) || queryString == "?")
            {
                return null;
            }

            var scanIndex = 0;
            if (queryString[0] == '?')
            {
                scanIndex = 1;
            }

            var textLength = queryString.Length;
            var equalIndex = queryString.IndexOf('=');
            if (equalIndex == -1)
            {
                equalIndex = textLength;
            }

            while (scanIndex < textLength)
            {
                var ampersandIndex = queryString.IndexOf('&', scanIndex);
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
                    var name = queryString[scanIndex..equalIndex];
                    var value = queryString.Substring(equalIndex + 1, ampersandIndex - equalIndex - 1);
                    var processedName = Uri.UnescapeDataString(name.Replace('+', ' '));
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
                        var value = queryString[scanIndex..ampersandIndex];
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

    public class EasyAuthRemoteAuthenticationContext : RemoteAuthenticationContext<RemoteAuthenticationState>
    {
        public string SelectedProvider { get; set; }
    }
}
