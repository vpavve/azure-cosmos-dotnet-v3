//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace CosmosBenchmark
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos;

    internal class BulkReadManyV3BenchmarkOperation : IBenchmarkOperation
    {
        private readonly Container container;
        private readonly string partitionKeyPath;

        private readonly string databaseName;
        private readonly string containerName;

        private readonly ItemOperation[] operations = createInputOperations();
        private readonly Dictionary<string, object> sampleJObject;

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

            this.sampleJObject = JsonHelper.Deserialize<Dictionary<string, object>>(sampleJson);
        }

        public virtual Task<OperationResult> ExecuteOnceAsync()
        {
            return this.ExecuteOnceAsyncInternal(useQuery: false);
        }

        public async Task<OperationResult> ExecuteOnceAsyncInternal(bool useQuery)
        {
            // ItemOperation[] itemOperations = createInputOperations();
            ItemOperation[] itemOperations = operations;

            Tuple<CosmosDiagnostics, TransactionalBatchOperationResult[]> manyResults = await this.container.ExecuteManyAsync(
                        itemOperations,
                        new TransactionalBatchRequestOptions() { UseQuery = useQuery },
                        CancellationToken.None);

            foreach (TransactionalBatchOperationResult result in manyResults.Item2)
            {
                if (result.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    System.Console.WriteLine($"Got status code: {result.StatusCode}");
                }
            }

            return new OperationResult()
            {
                DatabseName = databaseName,
                ContainerName = containerName,
                RuCharges = 0,
                CosmosDiagnostics = manyResults.Item1,
                LazyDiagnostics = () => manyResults.Item1.ToString(),
            };
        }

        private static ItemOperation[] createInputOperations()
        {
            int count = 100;

            ItemOperation[] itemOperations = new ItemOperation[count];
            for (int i = 0; i < count; i++)
            {
                itemOperations[i] = ItemOperation.Read(
                               Guid.NewGuid().ToString(),
                               Guid.NewGuid().ToString());
            }

            return itemOperations;
        }

        public async Task PrepareAsync()
        {
            // return Task.CompletedTask;
            foreach (ItemOperation op in operations)
            {
                this.sampleJObject["id"] = op.Id;
                this.sampleJObject[this.partitionKeyPath] = op.RawPartitionKey;

                using (MemoryStream input = JsonHelper.ToStream(this.sampleJObject))
                {
                    await this.container.CreateItemStreamAsync(
                        input,
                        new PartitionKey(this.sampleJObject[this.partitionKeyPath].ToString()));
                }
            }
        }
    }
}
