// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// ------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.Encryption
{
    using System;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Data.Encryption.Cryptography;

    /// <summary>
    /// CosmosClient with Encryption support.
    /// </summary>
    internal sealed class CosmosEncryptionKeyCache : IEncryptionKeyCache
    {
        private readonly CosmosClient cosmosClient;

        private readonly AsyncCache<string, ClientEncryptionKeyProperties> clientEncryptionKeyPropertiesCacheByKeyId;

        public CosmosEncryptionKeyCache(CosmosClient cosmosClient, EncryptionKeyStoreProvider encryptionKeyStoreProvider)
        {
            this.cosmosClient = cosmosClient ?? throw new ArgumentNullException(nameof(cosmosClient));
            this.EncryptionKeyStoreProvider = encryptionKeyStoreProvider ?? throw new ArgumentNullException(nameof(encryptionKeyStoreProvider));
            this.clientEncryptionKeyPropertiesCacheByKeyId = new AsyncCache<string, ClientEncryptionKeyProperties>();
        }

        public EncryptionKeyStoreProvider EncryptionKeyStoreProvider { get; }

        public override Database GetDatabase(string id)
        {
            return new EncryptionDatabase(this.cosmosClient.GetDatabase(id), this);
        }

        public async Task<ClientEncryptionKeyProperties> GetClientEncryptionKeyPropertiesAsync(
            string databaseRid,
            string clientEncryptionKeyId,
            CancellationToken cancellationToken = default,
            bool shouldForceRefresh = false)
        {
            if (string.IsNullOrEmpty(databaseRid))
            {
                throw new ArgumentNullException(nameof(databaseRid));
            }

            if (string.IsNullOrEmpty(clientEncryptionKeyId))
            {
                throw new ArgumentNullException(nameof(clientEncryptionKeyId));
            }

            // Client Encryption key Id is unique within a Database.
            string cacheKey = databaseRid + "|" + clientEncryptionKeyId;

            return await this.clientEncryptionKeyPropertiesCacheByKeyId.GetAsync(
                     cacheKey,
                     obsoleteValue: null,
                     async () => await this.FetchClientEncryptionKeyPropertiesAsync(databaseRid, clientEncryptionKeyId, cancellationToken),
                     cancellationToken,
                     forceRefresh: shouldForceRefresh);
        }

        private async Task<ClientEncryptionKeyProperties> FetchClientEncryptionKeyPropertiesAsync(
            string databaseRid,
            string clientEncryptionKeyId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ClientEncryptionKey clientEncryptionKey = this.GetDatabase(databaseRid).GetClientEncryptionKey(clientEncryptionKeyId);
            try
            {
                return await clientEncryptionKey.ReadAsync(cancellationToken: cancellationToken);
            }
            catch (CosmosException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new InvalidOperationException($"Encryption Based Container without Client Encryption Keys. Please make sure you have created the Client Encryption Keys:{ex.Message}. Please refer to https://aka.ms/CosmosClientEncryption for more details. ");
                }
                else
                {
                    throw;
                }
            }
        }
    }

    internal interface IEncryptionKeyCache : IDisposable
    {
        Task<ClientEncryptionKeyProperties> GetClientEncryptionKeyPropertiesAsync(
            string databaseRid,
            string clientEncryptionKeyId,
            CancellationToken cancellationToken = default,
            bool shouldForceRefresh = false);
    }
}
