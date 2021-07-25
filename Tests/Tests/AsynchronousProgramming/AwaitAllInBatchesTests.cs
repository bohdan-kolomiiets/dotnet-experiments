using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Tests.AsynchronousProgramming
{
    public class AwaitAllInBatchesTests
    {
        private readonly ITestOutputHelper _testOutput;

        public AwaitAllInBatchesTests(ITestOutputHelper testOutput)
        {
            _testOutput = testOutput;
        }


        /*
        --- v1 ---
        00:00:00.0161864: start task that will list for 1000 ms
        00:00:00.0173297: start task that will list for 2000 ms
        00:00:00.0173623: start task that will list for 3000 ms
        00:00:00.0173803: start task that will list for 4000 ms
        00:00:04.0279551: start task that will list for 5000 ms
        00:00:04.0280437: start task that will list for 6000 ms
        00:00:04.0280493: start task that will list for 7000 ms
        00:00:04.0280559: start task that will list for 8000 ms
        00:00:12.0285214: start task that will list for 9000 ms
        00:00:12.0286323: start task that will list for 10000 ms
        v1: 00:00:22.0450195
    
        --- v2 ---
        00:00:00.0061608: start task that will list for 1000 ms
        00:00:00.0071316: start task that will list for 2000 ms
        00:00:00.0072221: start task that will list for 3000 ms
        00:00:00.0072345: start task that will list for 4000 ms
        00:00:01.0141129: start task that will list for 5000 ms
        00:00:02.0180763: start task that will list for 6000 ms
        00:00:03.0186196: start task that will list for 7000 ms
        00:00:04.0164931: start task that will list for 8000 ms
        00:00:06.0285112: start task that will list for 9000 ms
        00:00:08.0264432: start task that will list for 10000 ms
        v2: 00:00:18.0477969

        Conclusion: 
        - v2 results in less or same execution time.
        - under certain tasks execution times and batching config, time will be the same (i.e : size: 10, batchSize: 5)
         */
        [Fact]
        public async Task Run()
        {
            var stopWatch = new Stopwatch();

            var asyncFunctions = Enumerable
                .Range(start: 1, count: 10)
                .Select<int, Func<Task>>(number => () => 
                {
                    var delay = number * 1000;
                    _testOutput.WriteLine($"{stopWatch.Elapsed}: start task that will list for {delay} ms");
                    return Task.Delay(number * 1000);
                });


            _testOutput.WriteLine($"--- v1 ---");
            stopWatch.Restart();
            await asyncFunctions.AwaitAllInBatchesV1(size: 4);
            _testOutput.WriteLine($"v1: {stopWatch.Elapsed}");
            _testOutput.WriteLine("");

            _testOutput.WriteLine($"--- v2 ---");
            stopWatch.Restart();
            await asyncFunctions.AwaitAllInBatchesV2(size: 4);
            _testOutput.WriteLine($"v2: {stopWatch.Elapsed}");
            _testOutput.WriteLine("");
        }
    }
}
