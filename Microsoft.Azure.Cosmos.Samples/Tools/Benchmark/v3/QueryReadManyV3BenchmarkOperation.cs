//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace CosmosBenchmark
{
    using System;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos;

    internal class QueryReadManyV3BenchmarkOperation : IBenchmarkOperation
    {
        private readonly Container container;
        private readonly string partitionKeyPath;

        private readonly string databaseName;
        private readonly string containerName;

        public QueryReadManyV3BenchmarkOperation(
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
            StringBuilder query = new StringBuilder(5*1024);
            query.Append("select * from c where ");

            int i=0;
            int count = 100;

            do
            {
                query.Append($" (c.{this.partitionKeyPath}=\"{Guid.NewGuid()}\" and c.id = \"{Guid.NewGuid()}\") ");
                i++;

                if (i < count)
                {
                    query.Append(" or " );
                }

            }
            while (i < count);

            FeedIterator feedIterator = this.container.GetItemQueryStreamIterator(
                        new QueryDefinition(query.ToString()),
                        null,
                        null);

            ResponseMessage responseMessage = null;
            while (feedIterator.HasMoreResults) 
            {
                responseMessage = await feedIterator.ReadNextAsync();
                if (responseMessage.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    System.Console.WriteLine($"Got status code: {responseMessage.StatusCode}");
                }
            }

            return new OperationResult()
            {
                DatabseName = databaseName,
                ContainerName = containerName,
                RuCharges = 0,
                CosmosDiagnostics = responseMessage.Diagnostics,
                LazyDiagnostics = () => responseMessage.Diagnostics.ToString(),
            };
        }

        public Task PrepareAsync()
        {
            return Task.CompletedTask;
        }
    }
}
