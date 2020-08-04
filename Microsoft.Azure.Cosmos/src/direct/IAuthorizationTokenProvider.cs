//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------
namespace Microsoft.Azure.Documents
{
    using Microsoft.Azure.Documents.Collections;
    using System;
    using System.Threading.Tasks;

    internal interface IAuthorizationTokenProvider : IDisposable
    {
        /// <summary>
        /// Generates a Authorization Token for a given resource type, address and action.
        /// </summary>
        ValueTask<IDisposableBytes> AuthorizeAsync(
            string resourceAddress,
            string resourceType,
            string requestVerb,
            INameValueCollection headers);

#if !COSMOSCLIENT
        Task AddSystemAuthorizationHeaderAsync(
            DocumentServiceRequest request,
            string federationId,
            string verb,
            string resourceId);
#endif

        public interface IDisposableBytes : IDisposable
        {
            public string GetDiagnosticsPayload();
        }
    }
}
