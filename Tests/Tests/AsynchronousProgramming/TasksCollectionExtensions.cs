using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Tests.AsynchronousProgramming
{

    public static class TasksCollectionExtensions
    {

        public static async Task AwaitAllInBatchesV1(
            this IEnumerable<Func<Task>> asyncFunctions,
            int size,
            Action<Exception>? exceptionHandler = null)
        {
            var asyncFunctionsWithResult = asyncFunctions
                .Select<Func<Task>, Func<Task<bool>>>(asyncFunction => () => asyncFunction().ContinueWith(task => true));

            await asyncFunctionsWithResult.AwaitAllInBatchesV1(size, exceptionHandler);
        }
       
        public static async Task<IEnumerable<T>> AwaitAllInBatchesV1<T>(
            this IEnumerable<Func<Task<T>>> asyncFunctions,
            int size,
            Action<Exception>? exceptionHandler = null)
        {
            exceptionHandler = exceptionHandler ?? _noExceptionHandling;

            var tasksBatches = asyncFunctions
                .Batch(size)
                .Select(async asyncFunctionsBatch =>
                {
                    var tasksbatch = asyncFunctionsBatch
                        .Select(async asyncFunction =>
                        {
                            try
                            {
                                return await asyncFunction();
                            }
                            catch (Exception exception)
                            {
                                exceptionHandler(exception);
                                return default;
                            }
                        })
                        .ToArray(); // if ienumerable is not evaluted, then when accessing Result below each task will be run again !!!
                    await Task.WhenAll(tasksbatch);

                    return tasksbatch
                        .Where(task => task.IsCompletedSuccessfully)
                        .Select(task => task.Result!);
                });

            var concurrentBag = new ConcurrentBag<T>();
            foreach(var tasksBatch in tasksBatches)
            {
                var results = await tasksBatch;
                foreach(var result in results) 
                {
                    concurrentBag.Add(result);
                }
            }

            return concurrentBag;
        }


        public static async Task AwaitAllInBatchesV2(
            this IEnumerable<Func<Task>> asyncFunctions,
            int size,
            Action<Exception>? exceptionHandler = null)
        {
            var asyncFunctionsWithResults = asyncFunctions
                .Select<Func<Task>, Func<Task<bool>>>(asyncFunction => () => asyncFunction().ContinueWith(task => true));

            await asyncFunctionsWithResults.AwaitAllInBatchesV2(size, exceptionHandler);
        }

        private static Action<Exception> _noExceptionHandling = exception => { };
        public static async Task<IEnumerable<T>> AwaitAllInBatchesV2<T>(
            this IEnumerable<Func<Task<T>>> asyncFunctions,
            int size,
            Action<Exception>? exceptionHandler = null)
        {
            exceptionHandler = exceptionHandler ?? _noExceptionHandling;

            var concurrentBag = new ConcurrentBag<T>();

            using (var semaphore = new SemaphoreSlim(size))
            {
                var semaphoreControlledTasks = asyncFunctions.Select(async asyncFunction =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        var result = await asyncFunction();
                        concurrentBag.Add(result);
                    }
                    catch (Exception exception)
                    {
                        exceptionHandler(exception);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                await Task.WhenAll(semaphoreControlledTasks);
            }

            return concurrentBag;
        }
    }
}
