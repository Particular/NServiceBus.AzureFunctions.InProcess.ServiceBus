namespace NServiceBus.AzureFunctions
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Transport;

    /// <summary>
    /// An NServiceBus endpoint which does not receive messages automatically but only handles messages explicitly passed to it
    /// by the caller.
    /// Instances of <see cref="ServerlessEndpoint{TExecutionContext, TConfiguration}" /> can be cached and are thread-safe.
    /// </summary>
    public abstract class ServerlessEndpoint<TExecutionContext, TConfiguration>
        where TConfiguration : ServerlessEndpointConfiguration
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
        /// <param name="configuration">The fully initialized configuration.</param>
        protected virtual Task Initialize(TConfiguration configuration)
        {
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
                        await Initialize(configuration).ConfigureAwait(false);
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

        readonly Func<TExecutionContext, TConfiguration> configurationFactory;

        readonly SemaphoreSlim semaphoreLock = new SemaphoreSlim(initialCount: 1, maxCount: 1);

        PipelineInvoker pipeline;
    }
}