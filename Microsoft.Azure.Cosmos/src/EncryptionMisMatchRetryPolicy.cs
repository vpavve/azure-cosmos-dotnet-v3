//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Cosmos
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Common;
    using Microsoft.Azure.Documents;

    /// <summary>
    /// EncryptionMisMatchRetryPolicy
    /// </summary>
    internal class EncryptionMisMatchRetryPolicy : IDocumentClientRetryPolicy
    {
        private const int MaxRetries = 1;

        private readonly CollectionCache clientCollectionCache;
        private readonly IDocumentClientRetryPolicy nextRetryPolicy;
        private readonly ContainerProperties containerProperties;

        private int retriesAttempted;

        public EncryptionMisMatchRetryPolicy(
            CollectionCache clientCollectionCache,
            ContainerProperties containerProperties,
            IDocumentClientRetryPolicy nextRetryPolicy)
        {
            this.clientCollectionCache = clientCollectionCache ?? throw new ArgumentNullException(nameof(clientCollectionCache));
            this.containerProperties = containerProperties ?? throw new ArgumentException(nameof(containerProperties));
            this.nextRetryPolicy = nextRetryPolicy;
        }

        public void OnBeforeSendRequest(DocumentServiceRequest request)
        {
            // NA
        }

        public Task<ShouldRetryResult> ShouldRetryAsync(ResponseMessage cosmosResponseMessage, CancellationToken cancellationToken)
        {
            ShouldRetryResult shouldRetryResult = this.ShouldRetryInternal(cosmosResponseMessage?.StatusCode,
                cosmosResponseMessage?.Headers.SubStatusCode);
            if (shouldRetryResult != null)
            {
                return Task.FromResult(shouldRetryResult);
            }

            if (this.nextRetryPolicy == null)
            {
                return Task.FromResult(ShouldRetryResult.NoRetry());
            }

            return this.nextRetryPolicy.ShouldRetryAsync(cosmosResponseMessage, cancellationToken);
        }

        public Task<ShouldRetryResult> ShouldRetryAsync(Exception exception, CancellationToken cancellationToken)
        {
            DocumentClientException clientException = exception as DocumentClientException;

            ShouldRetryResult shouldRetryResult = this.ShouldRetryInternal(
                clientException?.StatusCode,
                clientException?.GetSubStatus());

            if (shouldRetryResult != null)
            {
                return Task.FromResult(shouldRetryResult);
            }

            if (this.nextRetryPolicy == null)
            {
                return Task.FromResult(ShouldRetryResult.NoRetry());
            }

            return this.nextRetryPolicy.ShouldRetryAsync(exception, cancellationToken);
        }

        private ShouldRetryResult ShouldRetryInternal(
            HttpStatusCode? statusCode,
            SubStatusCodes? subStatusCode)
        {
            if (!statusCode.HasValue
                && (!subStatusCode.HasValue
                || subStatusCode.Value == SubStatusCodes.Unknown))
            {
                return null;
            }

            if (statusCode == HttpStatusCode.BadRequest
                && subStatusCode == (SubStatusCodes)1024 //Constants.IncorrectContainerRidSubStatus
                && this.retriesAttempted < MaxRetries)
            {
                // Force refres collection by removing the entries from both named and id based caches
                // this.clientCollectionCache.Refresh(resourceIdOrFullName, HttpConstants.Versions.CurrentVersion);
                this.retriesAttempted++;
                // return ShouldRetryResult.RetryAfter(TimeSpan.Zero);

                throw new NotImplementedException();
            }

            return null;
        }
    }
}
