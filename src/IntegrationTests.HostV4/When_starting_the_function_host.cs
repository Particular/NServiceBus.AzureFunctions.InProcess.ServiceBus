namespace ServiceBus.Tests
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Messaging.ServiceBus.Administration;
    using Microsoft.Azure.Amqp.Transport;
    using NUnit.Framework;

    public class When_starting_the_function_host
    {
        [Test]
        public async Task Should_not_blow_up()
        {
            var pathToFuncExe = Environment.GetEnvironmentVariable("PathToFuncExe");
            Assert.IsNotNull(pathToFuncExe, $"Environment variable 'PathToFuncExe' should be defined to run tests. When running locally this is usually 'C:\\Users\\MyUser\\AppData\\Local\\AzureFunctionsTools\\Releases\\4.30.0\\cli_x64\\func.exe'");

            var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsServiceBus");
            Assert.IsNotNull(connectionString, $"Environment variable 'AzureWebJobsServiceBus' should be defined to run tests.");

            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            var client = new ServiceBusAdministrationClient(connectionString);

            const string queueName = "inprocess-hostv4";
            const string topicName = "bundle-1";

            if (!await client.QueueExistsAsync(queueName, cancellationTokenSource.Token))
            {
                await client.CreateQueueAsync(queueName, cancellationTokenSource.Token);
            }

            if (!await client.TopicExistsAsync(topicName, cancellationTokenSource.Token))
            {
                await client.CreateTopicAsync(topicName, cancellationTokenSource.Token);
            }

            if (!await client.SubscriptionExistsAsync(topicName, queueName, cancellationTokenSource.Token))
            {
                var subscription = new CreateSubscriptionOptions(topicName, queueName)
                {
                    LockDuration = TimeSpan.FromMinutes(5),
                    ForwardTo = queueName,
                    EnableDeadLetteringOnFilterEvaluationExceptions = false,
                    MaxDeliveryCount = int.MaxValue,
                    EnableBatchedOperations = true,
                    UserMetadata = queueName
                };
                await client.CreateSubscriptionAsync(subscription, cancellationTokenSource.Token);
            }

            var functionRootDir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            var port = 7076; //Use non-standard port to avoid clashing when debugging locally
            var funcProcess = new Process();
            var httpClient = new HttpClient();
            var hasResult = false;
            var hostFailed = false;
            var handlerCalled = false;
            var handlerCalledCompletionSource = new TaskCompletionSource<bool>();

            cancellationTokenSource.Token.Register(state => ((TaskCompletionSource<bool>)state)
              .TrySetResult(false), handlerCalledCompletionSource);

            funcProcess.StartInfo.WorkingDirectory = functionRootDir.FullName;
            funcProcess.StartInfo.Arguments = $"start --port {port} --no-build --verbose";
            funcProcess.StartInfo.FileName = pathToFuncExe;

            funcProcess.StartInfo.UseShellExecute = false;
            funcProcess.StartInfo.RedirectStandardOutput = true;
            funcProcess.StartInfo.RedirectStandardError = true;
            funcProcess.StartInfo.CreateNoWindow = true;
            funcProcess.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                if (e.Data == null)
                {
                    return;
                }

                hostFailed = true;
                TestContext.Out.WriteLine(e.Data);

                cancellationTokenSource.Cancel();
            };
            funcProcess.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                if (e.Data == null)
                {
                    return;
                }

                TestContext.Out.WriteLine(e.Data);

                if (e.Data.Contains($"Handling {nameof(SomeEvent)}"))
                {
                    handlerCalledCompletionSource.SetResult(true);
                }
            };
            funcProcess.EnableRaisingEvents = true;
            funcProcess.Start();
            funcProcess.BeginOutputReadLine();
            funcProcess.BeginErrorReadLine();

            try
            {
                while (!cancellationTokenSource.IsCancellationRequested && !hasResult)
                {
                    try
                    {
                        var result = await httpClient.GetAsync($"http://localhost:{port}/api/InProcessHttpSenderV4", cancellationTokenSource.Token);

                        result.EnsureSuccessStatusCode();

                        hasResult = true;
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    catch (Exception ex)
                    {
                        TestContext.Out.WriteLine(ex.Message);
                        await Task.Delay(TimeSpan.FromSeconds(1));
                    }
                }

                handlerCalled = await handlerCalledCompletionSource.Task;

                funcProcess.Kill();
            }
            finally
            {
                try
                {
                    await funcProcess.WaitForExitAsync(cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    funcProcess.Kill();
                }
            }

            Assert.False(hostFailed, "Host should startup without errors");
            Assert.True(hasResult, "Http trigger should respond successfully");
            Assert.True(handlerCalled, "Message handlers should be called");
        }
    }
}