//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.Encryption
{
    using System;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using global::Azure;
    using Microsoft.Data.Encryption.Cryptography;

    internal sealed class EncryptionSettingForProperty
    {
        public string ClientEncryptionKeyId { get; }

        public EncryptionType EncryptionType { get; }

        private readonly string databaseRid;

        private readonly IEncryptionKeyCache encryptionKeyCache;
        private readonly EncryptionKeyStoreProvider encryptionKeyStoreProvider;

        public EncryptionSettingForProperty(
            string clientEncryptionKeyId,
            EncryptionType encryptionType,
            IEncryptionKeyCache encryptionKeyCache,
            EncryptionKeyStoreProvider encryptionKeyStoreProvider,
            string databaseRid)
        {
            this.ClientEncryptionKeyId = string.IsNullOrEmpty(clientEncryptionKeyId) ? throw new ArgumentNullException(nameof(clientEncryptionKeyId)) : clientEncryptionKeyId;
            this.encryptionKeyCache = encryptionKeyCache ?? throw new ArgumentNullException(nameof(encryptionKeyCache));
            this.databaseRid = string.IsNullOrEmpty(databaseRid) ? throw new ArgumentNullException(nameof(databaseRid)) : databaseRid;
            this.encryptionKeyStoreProvider = encryptionKeyStoreProvider ?? throw new ArgumentNullException(nameof(encryptionKeyStoreProvider));

            this.EncryptionType = encryptionType;
        }

        public async Task<AeadAes256CbcHmac256EncryptionAlgorithm> BuildEncryptionAlgorithmForSettingAsync(CancellationToken cancellationToken)
        {
            ClientEncryptionKeyProperties clientEncryptionKeyProperties = await this.encryptionKeyCache.GetClientEncryptionKeyPropertiesAsync(
                    databaseRid: this.databaseRid,
                    clientEncryptionKeyId: this.ClientEncryptionKeyId,
                    cancellationToken: cancellationToken);

            ProtectedDataEncryptionKey protectedDataEncryptionKey;

            try
            {
                // we pull out the Encrypted Data Encryption Key and build the Protected Data Encryption key
                // Here a request is sent out to unwrap using the Master Key configured via the Key Encryption Key.
                protectedDataEncryptionKey = this.BuildProtectedDataEncryptionKey(
                    clientEncryptionKeyProperties,
                    this.ClientEncryptionKeyId);
            }
            catch (RequestFailedException ex)
            {
                // The access to master key was probably revoked. Try to fetch the latest ClientEncryptionKeyProperties from the backend.
                // This will succeed provided the user has rewraped the Client Encryption Key with right set of meta data.
                // This is based on the AKV provider implementaion so we expect a RequestFailedException in case other providers are used in unwrap implementation.
                if (ex.Status == (int)HttpStatusCode.Forbidden)
                {
                    clientEncryptionKeyProperties = await this.encryptionKeyCache.GetClientEncryptionKeyPropertiesAsync(
                        databaseRid: this.databaseRid,
                        clientEncryptionKeyId: this.ClientEncryptionKeyId,
                        cancellationToken: cancellationToken,
                        shouldForceRefresh: true);

                    // just bail out if this fails.
                    protectedDataEncryptionKey = this.BuildProtectedDataEncryptionKey(
                        clientEncryptionKeyProperties,
                        this.ClientEncryptionKeyId);
                }
                else
                {
                    throw;
                }
            }

            AeadAes256CbcHmac256EncryptionAlgorithm aeadAes256CbcHmac256EncryptionAlgorithm = new AeadAes256CbcHmac256EncryptionAlgorithm(
                   protectedDataEncryptionKey,
                   this.EncryptionType);

            return aeadAes256CbcHmac256EncryptionAlgorithm;
        }

        private ProtectedDataEncryptionKey BuildProtectedDataEncryptionKey(
            ClientEncryptionKeyProperties clientEncryptionKeyProperties,
            string keyId)
        {
            KeyEncryptionKey keyEncryptionKey = KeyEncryptionKey.GetOrCreate(
               clientEncryptionKeyProperties.EncryptionKeyWrapMetadata.Name,
               clientEncryptionKeyProperties.EncryptionKeyWrapMetadata.Value,
               this.encryptionKeyStoreProvider);

            ProtectedDataEncryptionKey protectedDataEncryptionKey = ProtectedDataEncryptionKey.GetOrCreate(
                   keyId,
                   keyEncryptionKey,
                   clientEncryptionKeyProperties.WrappedDataEncryptionKey);

            return protectedDataEncryptionKey;
        }
    }
}