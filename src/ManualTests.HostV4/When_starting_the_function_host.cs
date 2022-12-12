namespace ServiceBus.Tests
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;
    using NUnit.Framework;

    public class When_starting_the_function_host
    {
        [Test]
        public async Task Should_not_blow_up()
        {
            var funcExeFolder = @"C:\Users\andre\AppData\Local\AzureFunctionsTools\Releases\4.30.0\cli_x64";
            var pathToFuncExe = Path.Combine(funcExeFolder, "func.exe");
            var functionRootDir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory)
                .Parent.Parent.Parent;

            var funcProcess = new Process();
            funcProcess.StartInfo.WorkingDirectory = functionRootDir.FullName;
            funcProcess.StartInfo.Arguments = "start --port 7072 --no-build";
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

            await funcProcess.WaitForExitAsync().ConfigureAwait(false);

            Assert.AreEqual(0, funcProcess.ExitCode);
        }
        static void DataReceived(object sender, DataReceivedEventArgs e)
        {

            string strMessage = e.Data;
            Console.WriteLine(strMessage);
        }
    }
}