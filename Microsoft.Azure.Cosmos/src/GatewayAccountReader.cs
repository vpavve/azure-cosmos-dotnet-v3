//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Cosmos
{
    using System;
    using System.Globalization;
    using System.Net.Http;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Routing;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Collections;
    using static Microsoft.Azure.Documents.IAuthorizationTokenProvider;

    internal sealed class GatewayAccountReader
    {
        private readonly ConnectionPolicy connectionPolicy;
        private readonly IAuthorizationTokenProvider authorizationTokenProvider;
        private readonly GatewayStoreClient gatewayHttpClient;
        private readonly Uri serviceEndpoint;

        public GatewayAccountReader(Uri serviceEndpoint,
                IAuthorizationTokenProvider authorizationTokenProvider,
                ConnectionPolicy connectionPolicy,
                GatewayStoreClient gatewayStoreClient)
        {
            this.gatewayHttpClient = gatewayStoreClient;
            this.serviceEndpoint = serviceEndpoint;
            this.authorizationTokenProvider = authorizationTokenProvider;
            this.connectionPolicy = connectionPolicy;
        }

        private async Task<AccountProperties> GetDatabaseAccountAsync(Uri serviceEndpoint)
        {
            INameValueCollection headers = new DictionaryNameValueCollection(StringComparer.Ordinal);
            (string authorizationToken, IDisposableBytes payload) = await AuthorizationHelper.GenerateKeyAuthorizationSignature(
                    HttpConstants.HttpMethods.Get,
                    serviceEndpoint,
                    headers,
                    this.authorizationTokenProvider);
            using (payload)
            {

            }

            headers.Set(HttpConstants.HttpHeaders.Authorization, authorizationToken);
            return await this.gatewayHttpClient.GetDatabaseAccountAsync(serviceEndpoint, headers);
        }

        public async Task<AccountProperties> InitializeReaderAsync()
        {
            AccountProperties databaseAccount = await GlobalEndpointManager.GetDatabaseAccountFromAnyLocationsAsync(
                this.serviceEndpoint, this.connectionPolicy.PreferredLocations, this.GetDatabaseAccountAsync);

            return databaseAccount;
        }
    }
}
