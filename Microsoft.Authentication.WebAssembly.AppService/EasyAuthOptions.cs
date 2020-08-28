using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Authentication.WebAssembly.AppService
{
    public class EasyAuthOptions
    {
        public IList<ExternalProvider> Providers { get; set; } = new List<ExternalProvider> {
            new ExternalProvider("github", "GitHub"),
            new ExternalProvider("twitter", "Twitter"),
            new ExternalProvider("facebook", "Facebook"),
            new ExternalProvider("google", "Google"),
            new ExternalProvider("aad", "Azure Active Directory")
        };
        public string AuthenticationDataUrl { get; set; } = "";
    }

    public class ExternalProvider
    {
        public ExternalProvider(string id, string name)
        {
            Id = id;
            DisplayName = name;
        }

        public string Id { get; set; }
        public string DisplayName { get; set; }
    }
}