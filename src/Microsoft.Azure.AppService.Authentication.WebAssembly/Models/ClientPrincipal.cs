// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Azure.AppService.Authentication.WebAssembly.Models;

public record ClientPrincipal(string IdentityProvider, string UserId, string UserDetails, IEnumerable<string> UserRoles);
