using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Tests.ParallelProgramming
{
    public class ParallelTests
    {
        private readonly ITestOutputHelper _testOutput;

        public ParallelTests(ITestOutputHelper testOutput)
        {
            _testOutput = testOutput;
        }


        /*
        500 | sync: 00:00:00.0015248
        500 | async: 00:00:00.0129884
        500 | parallel: 00:00:00.0220406 - overhead only increase execution time
    
        5000 | sync: 00:00:00.0407866
        5000 | async: 00:00:00.0784026
        5000 | parallel: 00:00:00.0224335 - start seeing superiority
    
        50000 | sync: 00:00:03.3972152
        50000 | async: 00:00:07.8702273
        50000 | parallel: 00:00:02.6542136
    
        200000 | sync: 00:01:06.7114577
        200000 | async: 00:02:06.4814287
        200000 | parallel: 00:00:37.1922937 - on big number, we can see well that parallel execution wins

         */
        [Fact]
        public async Task Run()
        {
            var iterationsCounts = new[] { 500, 5000, 50000, 200000 };

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            foreach (var iterationsCount in iterationsCounts)
            {
                stopWatch.Restart();
                SyncAverage(iterationsCount);
                _testOutput.WriteLine($"{iterationsCount} | sync: {stopWatch.Elapsed}");

                stopWatch.Restart();
                await AsyncAverage(iterationsCount);
                _testOutput.WriteLine($"{iterationsCount} | async: {stopWatch.Elapsed}");

                stopWatch.Restart();
                ParallelAverage(iterationsCount);
                _testOutput.WriteLine($"{iterationsCount} | parallel: {stopWatch.Elapsed}");

                _testOutput.WriteLine("");
            }
        }

        double SyncAverage(int iterations)
        {
            var val = Enumerable
                .Range(start: 1, count: iterations)
                .Select(v => Calculate(v))
                .Average();
            return val;
        }
        double ParallelAverage(int iterations)
        {
            var values = new ConcurrentBag<int>();

            var range = Enumerable.Range(start: 1, count: iterations);
            Parallel.ForEach(Partitioner.Create(range),
                body: val =>
                {
                    values.Add(Calculate(val));
                });

            return values.Average();
        }

        async Task<double> AsyncAverage(int iterations)
        {
            var tasks = Enumerable
                .Range(start: 1, count: iterations)
                .Select(async v => Calculate(v));

            await Task.WhenAll(tasks);

            return tasks.Select(task => task.Result).Average();
        }
        int Calculate(int iterations)
        {
            var val = 0;
            for (int i = 0; i < iterations; i++)
            {
                val += i % 2 == 0 ? iterations : -1 * iterations;
            }
            return val;
        }
    }
}
