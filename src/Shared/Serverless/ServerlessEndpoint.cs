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
        /// Thread-safe initialization method that allows to get access to the strongly typed configuration.
        /// </summary>
        /// <remarks>Will only be called once either when <see cref="Process"/>, <see cref="ProcessFailedMessage"/> or <see cref="InitializeEndpointIfNecessary"/> is called.</remarks>
        /// <param name="executionContext">The exection context.</param>
        /// <param name="configuration">The fully initialized configuration.</param>
        protected virtual Task Initialize(TExecutionContext executionContext, TConfiguration configuration)
        {
            //TODO: also support exe files like core?
            var binFiles = Directory.EnumerateFiles(
                Path.Combine(executionContext.ExecutionContext.FunctionAppDirectory, "bin"), 
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

                    assemblyLoadContext.LoadFromAssemblyName(assemblyName);
                }
                catch (Exception e)
                {
                    executionContext.Logger.LogDebug(e, "Failed to load assembly {0}. This error can be ignored if the assembly isn't required to execute the function.", binFile);
                }
            }

            return Task.CompletedTask;
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
                        await Initialize(executionContext, configuration).ConfigureAwait(false);
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

        static bool IsRuntimeAssembly(byte[] publicKeyToken)
        {
            var tokenString = BitConverter.ToString(publicKeyToken).Replace("-", string.Empty).ToLowerInvariant();

            //Compare token to known Microsoft tokens

            if (tokenString == "b77a5c561934e089")
            {
                return true;
            }

            if (tokenString == "7cec85d7bea7798e")
            {
                return true;
            }

            if (tokenString == "b03f5f7f11d50a3a")
            {
                return true;
            }

            if (tokenString == "31bf3856ad364e35")
            {
                return true;
            }

            if (tokenString == "cc7b13ffcd2ddd51")
            {
                return true;
            }

            if (tokenString == "adb9793829ddae60")
            {
                return true;
            }

            return false;
        }

        readonly Func<TExecutionContext, TConfiguration> configurationFactory;

        readonly SemaphoreSlim semaphoreLock = new SemaphoreSlim(initialCount: 1, maxCount: 1);

        PipelineInvoker pipeline;
    }
}