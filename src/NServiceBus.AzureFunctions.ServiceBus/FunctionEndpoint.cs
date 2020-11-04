using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus.Logging;

namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AzureFunctions.ServiceBus;
    using Extensibility;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Extensions.Logging;
    using Transport;
    using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

    /// <summary>
    /// An NServiceBus endpoint hosted in Azure Function which does not receive messages automatically but only handles
    /// messages explicitly passed to it by the caller.
    /// </summary>
    public class FunctionEndpoint
    {
        private readonly IServiceProvider serviceProvider;

        /// <summary>
        /// Function endpoint that is injected into Azure Function.
        /// </summary>
        public FunctionEndpoint(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Processes a message received from an AzureServiceBus trigger using the NServiceBus message pipeline.
        /// </summary>
        public async Task Process(Message message, ExecutionContext executionContext, ILogger functionsLogger = null)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionsLogger);

            var messageContext = CreateMessageContext(message);
            var functionExecutionContext = new FunctionExecutionContext(executionContext, functionsLogger);

            try
            {
                await Process(messageContext, functionExecutionContext, serviceProvider).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                var errorContext = new ErrorContext(
                    exception,
                    message.GetHeaders(),
                    messageContext.MessageId,
                    messageContext.Body,
                    new TransportTransaction(),
                    message.SystemProperties.DeliveryCount);

                var errorHandleResult = await ProcessFailedMessage(errorContext, functionExecutionContext, serviceProvider)
                    .ConfigureAwait(false);

                if (errorHandleResult == ErrorHandleResult.Handled)
                {
                    // return to signal to the Functions host it can complete the incoming message
                    return;
                }

                throw;
            }

            MessageContext CreateMessageContext(Message originalMessage)
            {
                return new MessageContext(
                    originalMessage.GetMessageId(),
                    originalMessage.GetHeaders(),
                    originalMessage.Body,
                    new TransportTransaction(),
                    new CancellationTokenSource(),
                    new ContextBag());
            }
        }

        /// <summary>
        /// Lets the NServiceBus pipeline process this message.
        /// </summary>
        private async Task Process(MessageContext messageContext, FunctionExecutionContext executionContext, IServiceProvider serviceProvider)
        {
            await InitializeEndpointIfNecessary(executionContext, serviceProvider, messageContext.ReceiveCancellationTokenSource.Token).ConfigureAwait(false);

            await pipeline.PushMessage(messageContext).ConfigureAwait(false);
        }

        /// <summary>
        /// Lets the NServiceBus pipeline process this failed message.
        /// </summary>
        private async Task<ErrorHandleResult> ProcessFailedMessage(ErrorContext errorContext, FunctionExecutionContext executionContext, IServiceProvider serviceProvider)
        {
            await InitializeEndpointIfNecessary(executionContext, serviceProvider).ConfigureAwait(false);

            return await pipeline.PushFailedMessage(errorContext).ConfigureAwait(false);
        }

        /// <summary>
        /// Allows to forcefully initialize the endpoint if it hasn't been initialized yet.
        /// </summary>
        /// <param name="executionContext">The execution context.</param>
        /// <param name="serviceProvider">Service provider supplied via Azure Function.</param>
        /// <param name="token">The cancellation token or default cancellation token.</param>
        private async Task InitializeEndpointIfNecessary(FunctionExecutionContext executionContext, IServiceProvider serviceProvider, CancellationToken token = default)
        {
            if (pipeline == null)
            {
                await semaphoreLock.WaitAsync(token).ConfigureAwait(false);
                try
                {
                    if (pipeline == null)
                    {
                        LoadAssemblies(executionContext);
                        LogManager.GetLogger("Previews").Info("NServiceBus.AzureFunctions.ServiceBus is a preview package. Preview packages are licensed separately from the rest of the Particular Software platform and have different support guarantees. You can view the license at https://particular.net/eula/previews and the support policy at https://docs.particular.net/previews/support-policy. Customer adoption drives whether NServiceBus.AzureFunctions.ServiceBus will be incorporated into the Particular Software platform. Let us know you are using it, if you haven't already, by emailing us at support@particular.net.");

                        var startableEndpoint = serviceProvider.GetService<IStartableEndpointWithExternallyManagedContainer>();
                        var endpoint = await startableEndpoint.Start(serviceProvider).ConfigureAwait(false);

                        pipeline = serviceProvider.GetService<PipelineInvoker>();
                    }
                }
                finally
                {
                    semaphoreLock.Release();
                }
            }
        }

        void LoadAssemblies(FunctionExecutionContext executionContext)
        {
            var binFiles = Directory.EnumerateFiles(
                AssemblyDirectoryResolver(executionContext),
                "*.dll",
                SearchOption.TopDirectoryOnly);

            var assemblyLoadContext = AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly());
            foreach (var binFile in binFiles)
            {
                try
                {
                    var assemblyName = AssemblyName.GetAssemblyName(binFile);
                    if (IsRuntimeAssembly(assemblyName.GetPublicKeyToken()))
                    {
                        continue;
                    }

                    // LoadFromAssemblyName works when actually running inside a function as FunctionAssemblyLoadContext probes the "bin" folder for the assembly name
                    // this doesn't work when running with a different AssemblyLoadContext (e.g. tests) and the assembly needs to be loaded by the full path instead.
                    assemblyLoadContext.LoadFromAssemblyPath(binFile);
                    //assemblyLoadContext.LoadFromAssemblyName(assemblyName);
                }
                catch (Exception e)
                {
                    executionContext.Logger.LogDebug(e, "Failed to load assembly {0}. This error can be ignored if the assembly isn't required to execute the function.", binFile);
                }
            }
        }

        static bool IsRuntimeAssembly(byte[] publicKeyToken)
        {
            var tokenString = BitConverter.ToString(publicKeyToken).Replace("-", string.Empty).ToLowerInvariant();

            switch (tokenString)
            {
                case "b77a5c561934e089": // Microsoft
                case "7cec85d7bea7798e":
                case "b03f5f7f11d50a3a":
                case "31bf3856ad364e35":
                case "cc7b13ffcd2ddd51":
                case "adb9793829ddae60":
                case "7e34167dcc6d6d8c": // Microsoft.Azure.ServiceBus
                case "50cebf1cceb9d05e": // Mono.Cecil
                case "30ad4fe6b2a6aeed": // Newtonsoft.Json
                case "9fc386479f8a226c": // NServiceBus
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Provides a function to locate the file system directory containing the binaries to be loaded and scanned.
        /// When using functions, assemblies are moved to a 'bin' folder within ExecutionContext.FunctionAppDirectory.
        /// </summary>
        protected Func<FunctionExecutionContext, string> AssemblyDirectoryResolver = functionExecutionContext => Path.Combine(functionExecutionContext.ExecutionContext.FunctionAppDirectory, "bin");

        readonly SemaphoreSlim semaphoreLock = new SemaphoreSlim(initialCount: 1, maxCount: 1);

        PipelineInvoker pipeline;
    }
}
