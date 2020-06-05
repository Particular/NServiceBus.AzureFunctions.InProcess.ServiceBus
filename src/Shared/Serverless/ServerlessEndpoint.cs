namespace NServiceBus.AzureFunctions
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Runtime.Loader;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Transport;

    /// <summary>
    /// An NServiceBus endpoint which does not receive messages automatically but only handles messages explicitly passed to it
    /// by the caller.
    /// Instances of <see cref="ServerlessEndpoint{TExecutionContext, TConfiguration}" /> can be cached and are thread-safe.
    /// </summary>
    public abstract class ServerlessEndpoint<TExecutionContext, TConfiguration>
        where TConfiguration : ServerlessEndpointConfiguration
        where TExecutionContext : FunctionExecutionContext
    {
        /// <summary>
        /// Create a new session based on the configuration factory provided.
        /// </summary>
        protected ServerlessEndpoint(Func<TExecutionContext, TConfiguration> configurationFactory)
        {
            this.configurationFactory = configurationFactory;
        }

        /// <summary>
        /// Lets the NServiceBus pipeline process this message.
        /// </summary>
        protected async Task Process(MessageContext messageContext, TExecutionContext executionContext)
        {
            await InitializeEndpointIfNecessary(executionContext, messageContext.ReceiveCancellationTokenSource.Token).ConfigureAwait(false);

            await pipeline.PushMessage(messageContext).ConfigureAwait(false);
        }

        /// <summary>
        /// Lets the NServiceBus pipeline process this failed message.
        /// </summary>
        protected async Task<ErrorHandleResult> ProcessFailedMessage(ErrorContext errorContext, TExecutionContext executionContext)
        {
            await InitializeEndpointIfNecessary(executionContext).ConfigureAwait(false);

            return await pipeline.PushFailedMessage(errorContext).ConfigureAwait(false);
        }

        /// <summary>
        /// Allows to forcefully initialize the endpoint if it hasn't been initialized yet.
        /// </summary>
        /// <param name="executionContext">The execution context.</param>
        /// <param name="token">The cancellation token or default cancellation token.</param>
        // ReSharper disable once MemberCanBePrivate.Global
        protected async Task InitializeEndpointIfNecessary(TExecutionContext executionContext, CancellationToken token = default)
        {
            if (pipeline == null)
            {
                await semaphoreLock.WaitAsync(token).ConfigureAwait(false);
                try
                {
                    if (pipeline == null)
                    {
                        var configuration = configurationFactory(executionContext);
                        LoadAssemblies(executionContext);
                        await Endpoint.Start(configuration.EndpointConfiguration).ConfigureAwait(false);

                        pipeline = configuration.PipelineInvoker;
                    }
                }
                finally
                {
                    semaphoreLock.Release();
                }
            }
        }

        void LoadAssemblies(TExecutionContext executionContext)
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

                    //TODO: skip Particular signed assemblies too?

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

        readonly Func<TExecutionContext, TConfiguration> configurationFactory;

        readonly SemaphoreSlim semaphoreLock = new SemaphoreSlim(initialCount: 1, maxCount: 1);

        PipelineInvoker pipeline;
    }
}