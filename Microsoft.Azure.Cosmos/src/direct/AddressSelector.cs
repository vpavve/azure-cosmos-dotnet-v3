//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------
namespace Microsoft.Azure.Documents
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Security.Cryptography;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Documents.Client;

    internal sealed class AddressSelector
    {
        private readonly IAddressResolver addressResolver;
        private readonly Protocol protocol;

        public AddressSelector(IAddressResolver addressResolver,
            Protocol protocol)
        {
            this.addressResolver = addressResolver;
            this.protocol = protocol;
        }

        public async Task<Tuple<IReadOnlyList<Uri>, AddressCacheToken>> ResolveAllUriAsync(
           DocumentServiceRequest request,
           bool includePrimary,
           bool forceRefresh)
        {
            Tuple<PerProtocolPartitionAddressInformation, AddressCacheToken> addressInfo = await this.ResolveAddressesAsync(request, forceRefresh);

            return Tuple.Create<IReadOnlyList<Uri>, AddressCacheToken>
                  (includePrimary ? addressInfo.Item1.ReplicaUris : addressInfo.Item1.NonPrimaryReplicaUris, addressInfo.Item2);
        }

        public async Task<Tuple<Uri, AddressCacheToken>> ResolvePrimaryUriAsync(
            DocumentServiceRequest request,
            bool forceAddressRefresh)
        {
            Tuple<PerProtocolPartitionAddressInformation, AddressCacheToken> addressInfo = await this.ResolveAddressesAsync(request, forceAddressRefresh);
            return Tuple.Create<Uri, AddressCacheToken>(addressInfo.Item1.GetPrimaryUri(request), addressInfo.Item2);
        }

        public async Task<Tuple<PerProtocolPartitionAddressInformation, AddressCacheToken>> ResolveAddressesAsync(
            DocumentServiceRequest request,
            bool forceAddressRefresh)
        {
            PartitionAddressInformation partitionAddressInformation =
                await this.addressResolver.ResolveAsync(request, forceAddressRefresh, CancellationToken.None);

            return Tuple.Create<PerProtocolPartitionAddressInformation, AddressCacheToken>
                (partitionAddressInformation.Get(this.protocol), partitionAddressInformation.AddressCacheToken);
        }
    }
}