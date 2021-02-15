//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace CosmosBenchmark
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos;

    internal class BulkReadManyV3BenchmarkOperation : IBenchmarkOperation
    {
        private readonly Container container;
        private readonly string partitionKeyPath;

        private readonly string databaseName;
        private readonly string containerName;

        public BulkReadManyV3BenchmarkOperation(
            CosmosClient cosmosClient,
            string dbName,
            string containerName,
            string partitionKeyPath,
#pragma warning disable IDE0060 // Remove unused parameter
            string sampleJson)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            this.databaseName = dbName;
            this.containerName = containerName;

            this.container = cosmosClient.GetContainer(this.databaseName, this.containerName);
            this.partitionKeyPath = partitionKeyPath.Replace("/", "");
        }

        public async Task<OperationResult> ExecuteOnceAsync()
        {
            ItemOperation[] itemOperations = new ItemOperation[100];
            for (int i=0; i < 100; i++)
            {
                itemOperations[i] = ItemOperation.Read(
                    new PartitionKey(Guid.NewGuid().ToString()), 
                               Guid.NewGuid().ToString());
            }

            TransactionalBatchOperationResult[] manyResults = await this.container.ExecuteManyAsync(
                        itemOperations,
                        null,
                        CancellationToken.None);

            foreach(TransactionalBatchOperationResult result in manyResults)
            {
                if (result.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    System.Console.WriteLine($"Got status code: {result.StatusCode}");
                }
            }

            return new OperationResult()
            {
                DatabseName = databaseName,
                ContainerName = containerName,
                RuCharges = 0,
                CosmosDiagnostics = null,
                LazyDiagnostics = () => string.Empty,
            };
        }

        public Task PrepareAsync()
        {
            return Task.CompletedTask;
        }
    }
}
