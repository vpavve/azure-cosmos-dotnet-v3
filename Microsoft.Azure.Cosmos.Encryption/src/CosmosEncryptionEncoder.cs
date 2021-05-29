//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.Encryption
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    internal class CosmosEncryptionEncoder : CosmosStreamEncoder
    {
        private readonly AsyncCache<string, EncryptionSettings> encryptionSettingsByContainerRid;
        private readonly EncryptionContainer encryptionContainer;

        public CosmosEncryptionEncoder(EncryptionContainer encryptionContainer)
        {
            this.encryptionContainer = encryptionContainer ?? throw new ArgumentNullException(nameof(encryptionContainer));
        }

        public async override Task<Stream> DecodeAsync(
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
                singleValueInitFunc: () => EncryptionSettings.CreateAsync(this.encryptionContainer, cancellationToken),
                cancellationToken: cancellationToken);

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
                singleValueInitFunc: () => EncryptionSettings.CreateAsync(this.encryptionContainer, cancellationToken),
                cancellationToken: cancellationToken);

            encodingContext.UpsertCustomHeader(EncryptionSettings.IsClientEncryptedHeader, bool.TrueString);
            encodingContext.UpsertCustomHeader(EncryptionSettings.IntendedCollectionHeader, encryptionSettings.ContainerRidValue);

            return await EncryptionProcessor.EncryptAsync(
                   streamPayload,
                   encryptionSettings,
                   cancellationToken);
        }
    }
}
