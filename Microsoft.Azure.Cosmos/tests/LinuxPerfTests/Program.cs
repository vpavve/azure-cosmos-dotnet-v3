using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using Microsoft.Azure.Cosmos;

namespace MyBenchmarks
{
    [KeepBenchmarkFiles]
    [RPlotExporter]
    [MarkdownExporterAttribute.Default]
    public class CosmosItemTest
    {
        private CosmosClient Client { get; set; }
        private CosmosContainer TestContaienr { get; set; }

        public CosmosItemTest()
        {
            Console.WriteLine("CTOR called");

            var builder = new CosmosClientBuilder("https://kksql01.documents-staging.windows-ppe.net:443/", "mhQtlzAFxgYrixdkmEPeruIz0VkB3epzXLoFtj165jYJH2GPqXv7QU3idHeeoLQ6955N7XO4IEV8fiTKV0WrSQ==");
            this.Client = builder.Build();
            this.TestContaienr = this.Client.Databases["QuickStarts"].Containers["Documents"];
        }

        [Benchmark]
        public async Task SingelDocReadTest()
        {
            string pk = "b40a729b-d95d-46da-a8ad-7a08a10d7cf2";
            string id = "b40a729b-d95d-46da-a8ad-7a08a10d7cf2";
            var response = await this.TestContaienr.Items.ReadItemStreamAsync(pk, id);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception("Failed");
            }
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {

            ManualConfig cfg = new ManualConfig();
            cfg.Add(JitOptimizationsValidator.DontFailOnError);
            cfg.Add(StatisticColumn.P90);
            cfg.Add(StatisticColumn.P95);
            cfg.Add(StatisticColumn.P100);
            cfg.Add(StatisticColumn.P50);
            cfg.Add(StatisticColumn.OperationsPerSecond);
            cfg.Add(MarkdownExporter.Default);
            cfg.Add(ConsoleLogger.Default);

            var job = Job.Default
                        .WithGcServer(true);
                        ////.WithGcForce(true)
                        ////.WithIterationTime(TimeInterval.FromSeconds(10))
                        ////.WithIterationCount(25);
            cfg.Add(new Job[] { job });

            var summary = BenchmarkRunner.Run<CosmosItemTest>(cfg);

            Console.ReadLine();
        }
    }
}