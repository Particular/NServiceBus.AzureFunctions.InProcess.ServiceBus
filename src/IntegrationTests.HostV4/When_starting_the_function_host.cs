namespace ServiceBus.Tests
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;

    public class When_starting_the_function_host
    {
        [Test]
        public async Task Should_not_blow_up()
        {
            var funcExeFolder = Environment.GetEnvironmentVariable("PathToFuncExe");
            Assert.IsNotNull(funcExeFolder, $"Environment variable 'PathToFuncExe' should be defined to run tests. When running locally this is usually 'C:\\Users\\MyUser\\AppData\\Local\\AzureFunctionsTools\\Releases\\4.30.0\\cli_x64'");

            var pathToFuncExe = Path.Combine(funcExeFolder, "func.exe");
            var functionRootDir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            var port = 7076; //Use non-standard port to avoid clashing when debugging locally
            var funcProcess = new Process();
            funcProcess.StartInfo.WorkingDirectory = functionRootDir.FullName;
            funcProcess.StartInfo.Arguments = $"start --port {port} --no-build --verbose";
            funcProcess.StartInfo.FileName = pathToFuncExe;

            funcProcess.StartInfo.UseShellExecute = false;
            funcProcess.StartInfo.RedirectStandardOutput = true;
            funcProcess.StartInfo.RedirectStandardError = true;
            funcProcess.StartInfo.CreateNoWindow = true;
            funcProcess.ErrorDataReceived += DataReceived;
            funcProcess.OutputDataReceived += DataReceived;
            funcProcess.EnableRaisingEvents = true;
            funcProcess.Start();
            funcProcess.BeginOutputReadLine();
            funcProcess.BeginErrorReadLine();


            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var httpClient = new HttpClient();
            var hasResult = false;

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

            Assert.True(hasResult);
        }
        static void DataReceived(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Data))
            {
                return;
            }

            TestContext.Out.WriteLine(e.Data);
        }
    }
}