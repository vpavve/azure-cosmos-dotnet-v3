//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Cosmos
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// CosmosStreamEncoder
    /// </summary>
    public abstract class CosmosStreamEncoder
    {
        /// <summary>
        /// Encodes
        /// </summary>
        /// <param name="inputStream"></param>
        /// <param name="containerProperties"></param>
        /// <param name="encodingContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Stream</returns>
        public abstract Task<Stream> EncodeAsync(Stream inputStream, 
            ContainerProperties containerProperties, 
            CosmosEncodingContext encodingContext,
            CancellationToken cancellationToken);

        /// <summary>
        /// Decode
        /// </summary>
        /// <param name="encodedStream"></param>
        /// <param name="containerProperties"></param>
        /// <param name="encodingContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Stream</returns>
        public abstract Task<Stream> DecodeAsync(Stream encodedStream,
            ContainerProperties containerProperties,
            CosmosEncodingContext encodingContext,
            CancellationToken cancellationToken);

        /// <summary>
        /// CosmosEncodingContext
        /// </summary>
        public class CosmosEncodingContext
        {
            /// <summary>
            /// SubPath
            /// </summary>
            public string SubPath { get; }

            /// <summary>
            /// Add specific encoding/decoding headers context that needs to be relayed to service
            /// </summary>
            public Dictionary<string, string> CustomHeaders { get; set; }
        }
    }
}
