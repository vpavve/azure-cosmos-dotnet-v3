//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Documents
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Core.Trace;
    using Microsoft.Azure.Documents.Rntbd;

    /// <summary>
    /// ConnectionStateListener listens to the connection reset event notification fired by the transport client
    /// and refreshes the Document client's address cache
    /// </summary>
    internal sealed class ConnectionStateListener : IConnectionStateListener
    {
        private readonly IAddressResolverExtension addressResolver;

        public ConcurrentDictionary<ServerKey, HashSet<AddressCacheToken>> partitionAddressCache { get; }

        public ConnectionStateListener(IAddressResolverExtension addressResolver)
        {
            this.partitionAddressCache = new ConcurrentDictionary<ServerKey, HashSet<AddressCacheToken>>();
            this.addressResolver = addressResolver;
        }

        public void OnConnectionEvent(ConnectionEvent connectionEvent, DateTime eventTime, ServerKey serverKey)
        {
            DefaultTrace.TraceInformation("OnConnectionEvent fired, connectionEvent :{0}, eventTime: {1}, serverKey: {2}",
                connectionEvent,
                eventTime,
                serverKey.ToString());

            if (connectionEvent == ConnectionEvent.ReadEof || connectionEvent == ConnectionEvent.ReadFailure)
            {
                Task updateCacheTask = Task.Run(
                    async () => await this.UpdateAddressCacheAsync(serverKey));

                updateCacheTask.ContinueWith(task =>
                {
                    DefaultTrace.TraceWarning("AddressCache update failed: {0}", task.Exception?.InnerException);
                }, TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        public void UpdateConnectionState(Uri serverAddress, AddressCacheToken addressCacheToken)
        {
            if (addressCacheToken != null)
            {
                this.UpdatePartitionAddressCache(serverAddress, addressCacheToken);
            }
        }

        public void UpdateConnectionState(IReadOnlyList<Uri> serverAddresses, AddressCacheToken addressCacheToken)
        {
            if (addressCacheToken != null)
            {
                foreach (Uri serverUri in serverAddresses)
                {
                    this.UpdatePartitionAddressCache(serverUri, addressCacheToken);
                }
            }
        }

        private async Task UpdateAddressCacheAsync(ServerKey serverKey)
        {
            HashSet<AddressCacheToken> addressCacheTokens;
            if (this.partitionAddressCache.TryGetValue(serverKey, out addressCacheTokens))
            {
                IReadOnlyList<AddressCacheToken> tokens = new List<AddressCacheToken>(addressCacheTokens);
                await this.addressResolver.UpdateAsync(tokens);
            }
            else
            {
                DefaultTrace.TraceInformation("Serverkey {0} not found in the partitionAddressCache", serverKey);
            }
        }

        private void UpdatePartitionAddressCache(Uri serverAddress, AddressCacheToken addressCacheToken)
        {
            DefaultTrace.TraceInformation("Adding {0} serverAddress key to partitionAddressCache with values partitionKeyRangeIdentity: {1}, serviceEndpoint: {2}",
                serverAddress,
                addressCacheToken.PartitionKeyRangeIdentity.ToHeader(),
                addressCacheToken.ServiceEndpoint);

            this.partitionAddressCache.AddOrUpdate(
               new ServerKey(serverAddress), new HashSet<AddressCacheToken>() { addressCacheToken },
               (serverKey, addressCacheTokens) =>
               {
                   addressCacheTokens.Add(addressCacheToken);
                   return addressCacheTokens;
               });
        }
    }
}
