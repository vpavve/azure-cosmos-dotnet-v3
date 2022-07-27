﻿namespace WebApp.AspNetCore.Controllers
{
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.Cosmos;
    using Microsoft.Azure.Cosmos.Fluent;
    using Microsoft.Azure.Cosmos.SDK.EmulatorTests;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using OpenTelemetry.Models;
    using WebApp.AspNetCore.Models;

    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> logger;
        private readonly Container container;
        
        public HomeController(ILogger<HomeController> logger, Container container)
        {
            this.logger = logger;
            this.container = container;
        }

        public IActionResult Index()
        {
            Task.Run(async () =>
            {
                // Create an item
                ToDoActivity testItem = ToDoActivity.CreateRandomToDoActivity("MyTestPkValue");
                ItemResponse<ToDoActivity> createResponse = await this.container.CreateItemAsync<ToDoActivity>(testItem);
                ToDoActivity testItemCreated = createResponse.Resource;

                // Read an Item
                await this.container.ReadItemAsync<ToDoActivity>(testItem.id, new Microsoft.Azure.Cosmos.PartitionKey(testItem.id));

                // Upsert an Item
                await this.container.UpsertItemAsync<ToDoActivity>(testItem);

                // Replace an Item
                await this.container.ReplaceItemAsync<ToDoActivity>(testItemCreated, testItemCreated.id.ToString());

                // Delete an Item
                await this.container.DeleteItemAsync<ToDoActivity>(testItem.id, new Microsoft.Azure.Cosmos.PartitionKey(testItem.id));

            });

            return this.View();
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
