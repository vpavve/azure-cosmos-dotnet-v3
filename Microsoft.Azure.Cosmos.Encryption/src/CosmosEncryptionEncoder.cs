//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.Encryption
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Data.Encryption.Cryptography;

    internal class CosmosEncryptionEncoder : CosmosStreamEncoder
    {
        private readonly AsyncCache<string, EncryptionSettings> encryptionSettingsByContainerRid;
        private readonly IEncryptionKeyCache encryptionKeyCache;
        private readonly EncryptionKeyStoreProvider encryptionKeyStoreProvider;

        public CosmosEncryptionEncoder(
                IEncryptionKeyCache encryptionKeyCache,
                EncryptionKeyStoreProvider encryptionKeyStoreProvider)
        {
            this.encryptionKeyCache = encryptionKeyCache ?? throw new ArgumentNullException(nameof(encryptionKeyCache));
            this.encryptionKeyStoreProvider = encryptionKeyStoreProvider ?? throw new ArgumentNullException(nameof(encryptionKeyStoreProvider));
            this.encryptionSettingsByContainerRid = new AsyncCache<string, EncryptionSettings>();
        }

        public async override Task<Stream> DecodeAsync(
            Stream streamPayload,
            ContainerProperties containerProperties,
            CosmosEncodingContext encodingContext,
            CancellationToken cancellationToken)
        {
            // For scenarios with no-body optimization
            // TODO: How about move to a wrapper which does it cosistencly across implementations and usages
            if (streamPayload == null ||
                streamPayload.Length == 0)
            {
                return streamPayload;
            }

            if (containerProperties.ClientEncryptionPolicy == null)
            {
                // By-pass when encryption policy not configured
                // TODO: Do service emit 400/1024 even if RID is not set? If not what's the impact of stale!
                return streamPayload;
            }

            // Currently there is no versioning for encryption policy, which the service can't enforce also
            // So the concurrency scenario is limited to container re-creations only -> SelfLink/RID will suffice
            EncryptionSettings encryptionSettings = await this.encryptionSettingsByContainerRid.GetAsync(
                containerProperties.ResourceId,
                obsoleteValue: null,
                singleValueInitFunc: () => EncryptionSettings.CreateAsync(
                                                    containerProperties,
                                                    this.encryptionKeyCache,
                                                    this.encryptionKeyStoreProvider,
                                                    cancellationToken),
                cancellationToken: cancellationToken);

            if (!encryptionSettings.PropertiesToEncrypt.Any())
            {
                return streamPayload;
            }

            return await EncryptionProcessor.DecryptAsync(
                   streamPayload,
                   encryptionSettings,
                   cancellationToken);
        }

        public async override Task<Stream> EncodeAsync(
            Stream streamPayload,
            ContainerProperties containerProperties,
            CosmosEncodingContext encodingContext,
            CancellationToken cancellationToken)
        {
            if (containerProperties.ClientEncryptionPolicy == null)
            {
                // By-pass when encryption policy not configured
                // TODO: Do service emit 400/1024 even if RID is not set? If not what's the impact of stale!
                return streamPayload;
            }

            // Currently there is no versioning for encryption policy, which the service can't enforce also
            // So the concurrency scenario is limited to container re-creations only -> SelfLink/RID will suffice
            EncryptionSettings encryptionSettings = await this.encryptionSettingsByContainerRid.GetAsync(
                containerProperties.ResourceId,
                obsoleteValue: null,
                singleValueInitFunc: () => EncryptionSettings.CreateAsync(
                                                    containerProperties,
                                                    this.encryptionKeyCache,
                                                    this.encryptionKeyStoreProvider,
                                                    cancellationToken),
                cancellationToken: cancellationToken);

            if (!encryptionSettings.PropertiesToEncrypt.Any())
            {
                // TODO: Remove heads (in-case of retry ??)
                return streamPayload;
            }

            encodingContext.UpsertCustomHeader(EncryptionSettings.IsClientEncryptedHeader, bool.TrueString);
            encodingContext.UpsertCustomHeader(EncryptionSettings.IntendedCollectionHeader, encryptionSettings.ContainerRidValue);

            // For scenarios with no-body optimization (Headers should still be included)
            // TODO: How about move to a wrapper which does it cosistencly across implementations and usages
            if (streamPayload == null ||
                streamPayload.Length == 0)
            {
                return streamPayload;
            }

            return await EncryptionProcessor.EncryptAsync(
                    streamPayload,
                    encryptionSettings,
                    cancellationToken);
        }
    }
}
