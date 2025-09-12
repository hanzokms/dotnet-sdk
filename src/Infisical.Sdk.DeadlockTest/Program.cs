using System;
using System.Threading;
using System.Threading.Tasks;
using Infisical.Sdk;
using Infisical.Sdk.Model;

namespace Infisical.Sdk.DeadlockTest
{
    // Custom SynchronizationContext that simulates WinForms behavior
    public class WinFormsSynchronizationContext : SynchronizationContext
    {
        private readonly Thread _mainThread;
        private readonly Queue<(SendOrPostCallback, object?)> _queue = new();
        private readonly object _lock = new();
        private volatile bool _running = true;

        public WinFormsSynchronizationContext()
        {
            _mainThread = Thread.CurrentThread;
        }

        public override void Post(SendOrPostCallback d, object? state)
        {
            lock (_lock)
            {
                _queue.Enqueue((d, state));
                Monitor.Pulse(_lock);
            }
        }

        public override void Send(SendOrPostCallback d, object? state)
        {
            if (Thread.CurrentThread == _mainThread)
            {
                d(state);
            }
            else
            {
                var executed = false;
                var exception = (Exception?)null;

                Post(_ =>
                {
                    try
                    {
                        d(state);
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                    }
                    finally
                    {
                        lock (_lock)
                        {
                            executed = true;
                            Monitor.Pulse(_lock);
                        }
                    }
                }, null);

                lock (_lock)
                {
                    while (!executed)
                        Monitor.Wait(_lock);
                }

                if (exception != null)
                    throw exception;
            }
        }

        public void ProcessMessages()
        {
            while (_running)
            {
                (SendOrPostCallback, object?) item;

                lock (_lock)
                {
                    while (_queue.Count == 0 && _running)
                        Monitor.Wait(_lock, 100);

                    if (!_running) break;
                    if (_queue.Count == 0) continue;

                    item = _queue.Dequeue();
                }

                try
                {
                    item.Item1(item.Item2);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception in message loop: {ex.Message}");
                }
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                _running = false;
                Monitor.PulseAll(_lock);
            }
        }
    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("=== Infisical SDK Deadlock Demonstration ===\n");

            // Test 1: Normal console app behavior (no deadlock)
            Console.WriteLine("1. Testing in normal console context...");
            TestNormalConsole();

            Console.WriteLine("\n" + new string('=', 50) + "\n");

            // Test 2: Simulated WinForms context (deadlock)
            Console.WriteLine("2. Testing in simulated WinForms context...");
            TestSimulatedWinForms();

            Console.WriteLine("\n" + new string('=', 50) + "\n");

            // Test 3: Proper async/await solution
            Console.WriteLine("3. Testing proper async/await solution...");
            TestAsyncAwaitSolution();

            Console.WriteLine("\n" + new string('=', 50) + "\n");

            // Test 4: Task.Run workaround
            Console.WriteLine("4. Testing Task.Run workaround...");
            TestTaskRunWorkaround();
        }

        private static void TestNormalConsole()
        {
            try
            {
                var settings = new InfisicalSdkSettingsBuilder()
                    .WithHostUri("https://app.infisical.com")
                    .Build();

                var client = new InfisicalClient(settings);

                Console.WriteLine("Using .Result in console app (works fine)...");
                var task = client.Auth().UniversalAuth().LoginAsync("fake-id", "fake-secret");

                // This works fine in console apps
                try
                {
                    var result = task.Result;
                    Console.WriteLine("✅ No deadlock occurred (but auth failed as expected)");
                }
                catch (AggregateException ex)
                {
                    Console.WriteLine($"✅ No deadlock - got expected auth error: {ex.InnerException?.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Unexpected error: {ex.Message}");
            }
        }

        private static void TestSimulatedWinForms()
        {
            var winformsContext = new WinFormsSynchronizationContext();
            var deadlockDetected = false;
            var completed = false;
            Exception? caughtException = null;

            // This will run the actual test on the UI thread simulation
            var testCompletionSource = new TaskCompletionSource<bool>();

            // Post the deadlock test to run on the "UI thread"
            winformsContext.Post(_ =>
            {
                try
                {
                    // Set this thread's synchronization context
                    SynchronizationContext.SetSynchronizationContext(winformsContext);

                    var settings = new InfisicalSdkSettingsBuilder()
                        .WithHostUri("https://app.infisical.com")
                        .Build();

                    var client = new InfisicalClient(settings);

                    Console.WriteLine("Using .Result in WinForms-like context...");
                    Console.WriteLine("⏳ This should demonstrate a deadlock scenario...");

                    // This is the problematic pattern - calling .Result on the UI thread
                    // where the continuation needs to return to the same thread
                    var loginTask = client.Auth().UniversalAuth().LoginAsync("fake-id", "fake-secret");

                    // This should cause a deadlock if the SDK doesn't use ConfigureAwait(false)
                    try
                    {
                        var result = loginTask.Result;
                        completed = true;
                        Console.WriteLine("✅ Completed without deadlock (SDK properly uses ConfigureAwait(false))");
                    }
                    catch (AggregateException ex)
                    {
                        completed = true;
                        Console.WriteLine($"✅ Completed with expected auth error: {ex.InnerException?.Message}");
                        Console.WriteLine("   No deadlock occurred - SDK properly uses ConfigureAwait(false)");
                    }
                }
                catch (Exception ex)
                {
                    caughtException = ex;
                    Console.WriteLine($"❌ Unexpected error: {ex.Message}");
                }
                finally
                {
                    testCompletionSource.SetResult(true);
                }
            }, null);

            // Start the message pump and wait for the test to complete or timeout
            var messagePumpTask = Task.Run(() => winformsContext.ProcessMessages());

            // Wait for test completion or timeout (deadlock detection)
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
            var completedTask = Task.WhenAny(testCompletionSource.Task, timeoutTask);

            if (completedTask.Result == timeoutTask)
            {
                deadlockDetected = true;
                Console.WriteLine("❌ DEADLOCK DETECTED - Test didn't complete within 5 seconds");
                Console.WriteLine("   This means the SDK is NOT using ConfigureAwait(false) properly");
                Console.WriteLine("   In a real WinForms app, the UI would freeze indefinitely");
            }

            // Clean up
            winformsContext.Stop();
            messagePumpTask.Wait(TimeSpan.FromSeconds(1));

            // Report final results
            if (deadlockDetected)
            {
                Console.WriteLine("\n🎯 TEST RESULT: DEADLOCK DETECTED!");
                Console.WriteLine("   ❌ The SDK needs ConfigureAwait(false) on all await calls");
                Console.WriteLine("   ❌ Using .Result in WinForms/WPF will freeze the UI");
            }
            else if (completed)
            {
                Console.WriteLine("\n✅ TEST RESULT: NO DEADLOCK DETECTED!");
                Console.WriteLine("   ✅ The SDK properly uses ConfigureAwait(false)");
                Console.WriteLine("   ✅ Using .Result in WinForms/WPF should be safe");
            }
            else if (caughtException != null)
            {
                Console.WriteLine($"\n❌ TEST RESULT: Test failed with exception: {caughtException.Message}");
            }
        }

        private static void TestAsyncAwaitSolution()
        {
            Task.Run(async () =>
            {
                try
                {
                    var settings = new InfisicalSdkSettingsBuilder()
                        .WithHostUri("https://app.infisical.com")
                        .Build();

                    var client = new InfisicalClient(settings);

                    Console.WriteLine("Using proper async/await pattern...");

                    try
                    {
                        var result = await client.Auth().UniversalAuth().LoginAsync("fake-id", "fake-secret");
                        Console.WriteLine("✅ Async/await completed successfully");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"✅ Async/await completed with expected error: {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Unexpected error: {ex.Message}");
                }
            }).Wait();
        }

        private static void TestTaskRunWorkaround()
        {
            Task.Run(async () =>
            {
                try
                {
                    var settings = new InfisicalSdkSettingsBuilder()
                        .WithHostUri("https://app.infisical.com")
                        .Build();

                    var client = new InfisicalClient(settings);

                    Console.WriteLine("Using Task.Run workaround...");

                    try
                    {
                        await Task.Run(async () =>
                        {
                            var result = await client.Auth().UniversalAuth().LoginAsync("fake-id", "fake-secret");
                            return result;
                        });
                        Console.WriteLine("✅ Task.Run workaround completed successfully");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"✅ Task.Run workaround completed with expected error: {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Unexpected error: {ex.Message}");
                }
            }).Wait();
        }
    }
}
