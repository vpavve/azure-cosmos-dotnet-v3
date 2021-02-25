//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace CosmosBenchmark
{
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos;

    internal class QueryReadManyV3BenchmarkOperation : BulkReadManyV3BenchmarkOperation
    {
        public QueryReadManyV3BenchmarkOperation(
            CosmosClient cosmosClient,
            string dbName,
            string containerName,
            string partitionKeyPath,
#pragma warning disable IDE0060 // Remove unused parameter
            string sampleJson) : base (cosmosClient, dbName, containerName, partitionKeyPath, sampleJson)
#pragma warning restore IDE0060 // Remove unused parameter
        {
        }

        public override Task<OperationResult> ExecuteOnceAsync()
        {
            return this.ExecuteOnceAsyncInternal(useQuery: true);
        }
    }
}
