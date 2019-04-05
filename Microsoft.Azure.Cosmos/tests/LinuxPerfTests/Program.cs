using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
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
            Console.WriteLine("Is BenchMark.Net?");
            bool b = Boolean.Parse(Console.ReadLine());
            if (b)
            {
                ManualConfig cfg = new ManualConfig();
                cfg.Add(JitOptimizationsValidator.DontFailOnError);
                cfg.Add(StatisticColumn.P50);
                cfg.Add(StatisticColumn.P90);
                cfg.Add(StatisticColumn.P95);
                cfg.Add(StatisticColumn.P100);
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

                return;
            }

            Console.WriteLine($"Enter concurrency factor");
            int concurrency = int.Parse(Console.ReadLine());

            Console.WriteLine($"Enter ops-per-thread");
            int opsPerThread = int.Parse(Console.ReadLine());

            Console.WriteLine($"Enter #iterations");
            int iter = int.Parse(Console.ReadLine());

            CosmosItemTest test = new CosmosItemTest();

            // Warm-up run
            Program.PrintStats(concurrency, 10, () => test.SingelDocReadTest(), 1, true).Wait();

            // Real run
            Program.PrintStats(concurrency, opsPerThread, () => test.SingelDocReadTest(), iter).Wait();

            Console.ReadLine();
        }

        public static async Task PrintStats(int concurrency, 
                int opsPerThread,
                Func<Task> op,
                int iter,
                bool warmup = false)
        {
            IEnumerable<double> result = new List<double>();

            for (int index = 0; index < iter; index++)
            {
                long[][] latencies = new long[concurrency][];

                Task[] concurrentTasks = new Task[concurrency];
                for (int i = 0; i < concurrency; i++)
                {
                    latencies[i] = new long[opsPerThread];

                    concurrentTasks[i] = Program.ExecAsync(opsPerThread, op, latencies[i]);
                }

                await Task.WhenAll(concurrentTasks);

                result = result.Concat(Program.SingleEnum(latencies));
            }

            if (!warmup)
            {
                var orderedResult = result.OrderBy(e => e).ToArray();

                Console.WriteLine($"P50    -> {Program.Percentile(orderedResult, 50)}");
                Console.WriteLine($"P80    -> {Program.Percentile(orderedResult, 80)}");
                Console.WriteLine($"P90    -> {Program.Percentile(orderedResult, 90)}");
                Console.WriteLine($"P95    -> {Program.Percentile(orderedResult, 95)}");
                Console.WriteLine($"P99    -> {Program.Percentile(orderedResult, 99)}");
                Console.WriteLine($"P99.9  -> {Program.Percentile(orderedResult, 99.9)}");
                Console.WriteLine($"P99.99 -> {Program.Percentile(orderedResult, 99.99)}");
            }
        }

        private static IEnumerable<double> SingleEnum(long[][] data)
        {
            for(int i = 0; i< data.Length; i++)
            {
                for(int j=0; j< data[i].Length; j++)
                {
                    yield return data[i][j];
                }
            }
        }

        private static async Task<int> ExecAsync(
            int loopCount, 
            Func<Task> op,
            long[] results
            )
        {
            int failCount = 0;
            for (int i = 0; i < loopCount; i++)
            {
                Stopwatch sw = Stopwatch.StartNew();
                try
                {
                    await op();
                    sw.Stop();
                }
                catch (Exception)
                {
                    if (sw.IsRunning)
                    {
                        sw.Stop();
                    }

                    // Exception
                    failCount++;
                }

                results[i] = sw.ElapsedMilliseconds;
            }

            return failCount;
        }

        /// <summary>
        /// Calculates the Nth percentile from the set of values
        /// </summary>
        /// <remarks>
        /// The implementation is expected to be consistent with the one from Excel.
        /// It's a quite common to export bench output into .csv for further analysis 
        /// And it's a good idea to have same results from all tools being used.
        /// </remarks>
        /// <param name="sortedValues">Sequence of the values to be calculated</param>
        /// <param name="percentile">Value in range 0..100</param>
        /// <returns>Percentile from the set of values</returns>
        // Based on: http://stackoverflow.com/a/8137526
        private static double Percentile(IReadOnlyList<double> sortedValues, double percentile)
        {
            if (sortedValues == null)
                throw new ArgumentNullException(nameof(sortedValues));
            if (percentile < 0 || percentile > 100)
            {
                throw new ArgumentOutOfRangeException(
                     nameof(percentile), percentile,
                     "The percentile arg should be in range of 0 - 100.");
            }

            if (sortedValues.Count == 0)
                return 0;

            // DONTTOUCH: the following code was taken from http://stackoverflow.com/a/8137526 and it is proven
            // to work in the same way the excel's counterpart does.
            // So it's better to leave it as it is unless you do not want to reimplement it from scratch:)
            double realIndex = percentile / 100.0 * (sortedValues.Count - 1);
            int index = (int)realIndex;
            double frac = realIndex - index;
            if (index + 1 < sortedValues.Count)
                return sortedValues[index] * (1 - frac) + sortedValues[index + 1] * frac;
            return sortedValues[index];
        }
    }
}