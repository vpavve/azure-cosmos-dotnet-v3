namespace OpenTelemetry.Controllers
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.Cosmos;
    using Microsoft.Extensions.Logging;
    using Models;
    using WebApp.AspNetCore.Controllers;
    using WebApp.AspNetCore.Models;
    using static Azure.Core.HttpHeader;

    public class StreamOperationController : Controller
    {
        private readonly ILogger<HomeController> logger;
        private readonly Container container;
        private readonly SuccessViewModel successModel = new SuccessViewModel();

        public StreamOperationController(ILogger<HomeController> logger, Container container)
        {
            this.logger = logger;
            this.container = container;
        }

        public IActionResult Index()
        {
            Task.Run(async () =>
            {
                // Create an item
                var testItem = new { id = "MyTestItemId", partitionKeyPath = "MyTestPkValue", details = "it's working", status = "done" };
                await this.container
                    .CreateItemStreamAsync(this.ToStream(testItem),
                    new PartitionKey(testItem.id));

                //Upsert an Item
                await this.container.UpsertItemStreamAsync(this.ToStream(testItem), new PartitionKey(testItem.id));

                //Read an Item
                await this.container.ReadItemStreamAsync(testItem.id, new PartitionKey(testItem.id));

                //Replace an Item
                await this.container.ReplaceItemStreamAsync(this.ToStream(testItem), testItem.id, new PartitionKey(testItem.id));

                // Patch an Item
                List<PatchOperation> patch = new List<PatchOperation>()
            {
                PatchOperation.Add("/new", "patched")
            };
                await this.container.PatchItemStreamAsync(
                    partitionKey: new PartitionKey(testItem.id),
                    id: testItem.id,
                    patchOperations: patch);

                //Delete an Item
                await this.container.DeleteItemStreamAsync(testItem.id, new PartitionKey(testItem.id));


            });

            this.successModel.StreamOpsMessage = "Stream Operation Triggered Successfully";

            return this.View(this.successModel);
        }

        internal Stream ToStream<T>(T input)
        {
            CosmosSerializer serializer = this.GetSerializer<T>();
            return serializer.ToStream<T>(input);
        }

        public IActionResult Privacy()
        {
            return this.View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return this.View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? this.HttpContext.TraceIdentifier });
        }
    }
}
