// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Azure.Functions.Authentication.WebAssembly .Models;

namespace Microsoft.Azure.Functions.Authentication.WebAssembly 
{
    // A simple in-memory storage model for caching auth data
    class EasyAuthMemoryStorage
    {
        public AuthenticationData AuthenticationData { get; private set; }

        public void SetAuthenticationData(AuthenticationData data)
        {
            this.AuthenticationData = data;
        }
    }
}