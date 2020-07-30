//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Documents
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    internal interface IAddressResolverExtension : IAddressResolver
    {
        Task UpdateAsync(
            IReadOnlyList<AddressCacheToken> addressCacheTokens,
            CancellationToken cancellationToken = default(CancellationToken));
    }
}
