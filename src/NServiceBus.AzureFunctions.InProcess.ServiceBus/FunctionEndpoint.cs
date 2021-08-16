namespace NServiceBus
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Runtime.Loader;
    using System.Threading;
    using System.Threading.Tasks;
    using AzureFunctions.InProcess.ServiceBus;
    using Extensibility;
    using Logging;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging;
    using Transport;
    using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;
    using IMessageReceiver = Microsoft.Azure.ServiceBus.Core.IMessageReceiver;

    /// <summary>
    /// An NServiceBus endpoint hosted in Azure Function which does not receive messages automatically but only handles
    /// messages explicitly passed to it by the caller.
    /// </summary>
    public class FunctionEndpoint : IFunctionEndpoint
    {
        // This ctor is used for the FunctionsHostBuilder scenario where the endpoint is created already during configuration time using the function host's container.
        internal FunctionEndpoint(IStartableEndpointWithExternallyManagedContainer externallyManagedContainerEndpoint, ServiceBusTriggeredEndpointConfiguration configuration, IServiceProvider serviceProvider)
        {
            this.configuration = configuration;
            endpointFactory = _ => externallyManagedContainerEndpoint.Start(serviceProvider);
        }

        /// <summary>
        /// Processes a message received from an AzureServiceBus trigger using the NServiceBus message pipeline. This method will lookup the <see cref="ServiceBusTriggerAttribute.AutoComplete"/> setting to determine whether to use transactional or non-transactional processing.
        /// </summary>
        Task IFunctionEndpoint.Process(Message message, ExecutionContext executionContext, IMessageReceiver messageReceiver, ILogger functionsLogger) =>
            ReflectionHelper.GetAutoCompleteValue()
                ? ProcessNonTransactional(message, executionContext, messageReceiver, functionsLogger)
                : ProcessTransactional(message, executionContext, messageReceiver, functionsLogger);

        /// <summary>
        /// Processes a message received from an AzureServiceBus trigger using the NServiceBus message pipeline. All messages are committed transactionally with the successful processing of the incoming message.
        /// <remarks>Requires <see cref="ServiceBusTriggerAttribute.AutoComplete"/> to be set to false!</remarks>
        /// </summary>
        public async Task ProcessTransactional(Message message, ExecutionContext executionContext, IMessageReceiver messageReceiver, ILogger functionsLogger = null)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionsLogger);

            var functionExecutionContext = new FunctionExecutionContext(executionContext, functionsLogger);

            try
            {
                await InitializeEndpointIfNecessary(functionExecutionContext, CancellationToken.None)
                    .ConfigureAwait(false);

                await Process(message, new MessageReceiverTransactionStrategy(message, messageReceiver), pipeline)
                    .ConfigureAwait(false);
            }
            catch (Exception)
            {
                // abandon message outside of a transaction scope to ensure the abandon operation can't be rolled back
                await messageReceiver.AbandonAsync(message.SystemProperties.LockToken).ConfigureAwait(false);
                throw;
            }
        }

        /// <summary>
        /// Processes a message received from an AzureServiceBus trigger using the NServiceBus message pipeline.
        /// </summary>
        public async Task ProcessNonTransactional(Message message, ExecutionContext executionContext, IMessageReceiver messageReceiver, ILogger functionsLogger = null)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionsLogger);

            var functionExecutionContext = new FunctionExecutionContext(executionContext, functionsLogger);

            await InitializeEndpointIfNecessary(functionExecutionContext, CancellationToken.None)
                .ConfigureAwait(false);

            await Process(message, NoTransactionStrategy.Instance, pipeline)
                .ConfigureAwait(false);

        }

        /// <summary>
        /// Processes a message received from an AzureServiceBus trigger using the NServiceBus message pipeline.
        /// </summary>
        [ObsoleteEx(
            ReplacementTypeOrMember = "Process(Message, ExecutionContext, IMessageReceiver, ILogger)",
            TreatAsErrorFromVersion = "2",
            RemoveInVersion = "3")]
        public Task Process(Message message, ExecutionContext executionContext, ILogger functionsLogger = null)
        {
            throw new NotImplementedException();
        }

        internal static async Task Process(Message message, ITransactionStrategy transactionStrategy, PipelineInvoker pipeline)
        {
            var messageId = message.GetMessageId();

            try
            {
                using (var transaction = transactionStrategy.CreateTransaction())
                {
                    var transportTransaction = transactionStrategy.CreateTransportTransaction(transaction);
                    var messageContext = CreateMessageContext(transportTransaction);

                    await pipeline.PushMessage(messageContext).ConfigureAwait(false);

                    await transactionStrategy.Complete(transaction).ConfigureAwait(false);

                    transaction?.Commit();
                }
            }
            catch (Exception exception)
            {
                using (var transaction = transactionStrategy.CreateTransaction())
                {
                    var transportTransaction = transactionStrategy.CreateTransportTransaction(transaction);
                    var errorContext = new ErrorContext(
                        exception,
                        message.GetHeaders(),
                        messageId,
                        message.Body,
                        transportTransaction,
                        message.SystemProperties.DeliveryCount,
                        new ContextBag());

                    var errorHandleResult = await pipeline.PushFailedMessage(errorContext).ConfigureAwait(false);

                    if (errorHandleResult == ErrorHandleResult.Handled)
                    {
                        await transactionStrategy.Complete(transaction).ConfigureAwait(false);

                        transaction?.Commit();
                        return;
                    }

                    throw;
                }
            }

            MessageContext CreateMessageContext(TransportTransaction transportTransaction) =>
                new MessageContext(
                    messageId,
                    message.GetHeaders(),
                    message.Body,
                    transportTransaction,
                    new ContextBag());
        }

        /// <summary>
        /// Allows to forcefully initialize the endpoint if it hasn't been initialized yet.
        /// </summary>
        /// <param name="executionContext">The execution context.</param>
        /// <param name="token">The cancellation token or default cancellation token.</param>
        async Task InitializeEndpointIfNecessary(FunctionExecutionContext executionContext, CancellationToken token = default)
        {
            if (pipeline == null)
            {
                await semaphoreLock.WaitAsync(token).ConfigureAwait(false);
                try
                {
                    if (pipeline == null)
                    {
                        endpoint = await endpointFactory(executionContext).ConfigureAwait(false);

                        pipeline = configuration.PipelineInvoker;
                    }
                }
                finally
                {
                    semaphoreLock.Release();
                }
            }
        }

        /// <inheritdoc />
        public async Task Send(object message, SendOptions options, ExecutionContext executionContext, ILogger functionsLogger = null)
        {
            await InitializeEndpointUsedOutsideHandlerIfNecessary(executionContext, functionsLogger).ConfigureAwait(false);

            await endpoint.Send(message, options).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task Send(object message, ExecutionContext executionContext, ILogger functionsLogger = null)
        {
            return Send(message, new SendOptions(), executionContext, functionsLogger);
        }

        /// <inheritdoc />
        public async Task Send<T>(Action<T> messageConstructor, SendOptions options, ExecutionContext executionContext, ILogger functionsLogger = null)
        {
            await InitializeEndpointUsedOutsideHandlerIfNecessary(executionContext, functionsLogger).ConfigureAwait(false);

            await endpoint.Send(messageConstructor, options).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task Send<T>(Action<T> messageConstructor, ExecutionContext executionContext, ILogger functionsLogger = null)
        {
            return Send(messageConstructor, new SendOptions(), executionContext, functionsLogger);
        }

        /// <inheritdoc />
        public async Task Publish(object message, PublishOptions options, ExecutionContext executionContext, ILogger functionsLogger = null)
        {
            await InitializeEndpointUsedOutsideHandlerIfNecessary(executionContext, functionsLogger).ConfigureAwait(false);

            await endpoint.Publish(message, options).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task Publish<T>(Action<T> messageConstructor, PublishOptions options, ExecutionContext executionContext, ILogger functionsLogger = null)
        {
            await InitializeEndpointUsedOutsideHandlerIfNecessary(executionContext, functionsLogger).ConfigureAwait(false);

            await endpoint.Publish(messageConstructor, options).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task Publish(object message, ExecutionContext executionContext, ILogger functionsLogger = null)
        {
            await InitializeEndpointUsedOutsideHandlerIfNecessary(executionContext, functionsLogger).ConfigureAwait(false);

            await endpoint.Publish(message).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task Publish<T>(Action<T> messageConstructor, ExecutionContext executionContext, ILogger functionsLogger = null)
        {
            await InitializeEndpointUsedOutsideHandlerIfNecessary(executionContext, functionsLogger).ConfigureAwait(false);

            await endpoint.Publish(messageConstructor).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task Subscribe(Type eventType, SubscribeOptions options, ExecutionContext executionContext, ILogger functionsLogger = null)
        {
            await InitializeEndpointUsedOutsideHandlerIfNecessary(executionContext, functionsLogger).ConfigureAwait(false);

            await endpoint.Subscribe(eventType, options).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task Subscribe(Type eventType, ExecutionContext executionContext, ILogger functionsLogger = null)
        {
            await InitializeEndpointUsedOutsideHandlerIfNecessary(executionContext, functionsLogger).ConfigureAwait(false);

            await endpoint.Subscribe(eventType).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task Unsubscribe(Type eventType, UnsubscribeOptions options, ExecutionContext executionContext, ILogger functionsLogger = null)
        {
            await InitializeEndpointUsedOutsideHandlerIfNecessary(executionContext, functionsLogger).ConfigureAwait(false);

            await endpoint.Unsubscribe(eventType, options).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task Unsubscribe(Type eventType, ExecutionContext executionContext, ILogger functionsLogger = null)
        {
            await InitializeEndpointUsedOutsideHandlerIfNecessary(executionContext, functionsLogger).ConfigureAwait(false);

            await endpoint.Unsubscribe(eventType).ConfigureAwait(false);
        }

        async Task InitializeEndpointUsedOutsideHandlerIfNecessary(ExecutionContext executionContext, ILogger functionsLogger)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionsLogger);

            var functionExecutionContext = new FunctionExecutionContext(executionContext, functionsLogger);

            await InitializeEndpointIfNecessary(functionExecutionContext).ConfigureAwait(false);
        }

        internal static void LoadAssemblies(string assemblyDirectory)
        {
            var binFiles = Directory.EnumerateFiles(
                assemblyDirectory,
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
                    LogManager.GetLogger<FunctionEndpoint>().DebugFormat(
                        "Failed to load assembly {0}. This error can be ignored if the assembly isn't required to execute the function.{1}{2}",
                        binFile, Environment.NewLine, e);
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
                case "23ec7fc2d6eaa4a5": // Microsoft.Data.SqlClient
                case "50cebf1cceb9d05e": // Mono.Cecil
                case "30ad4fe6b2a6aeed": // Newtonsoft.Json
                case "9fc386479f8a226c": // NServiceBus
                    return true;
                default:
                    return false;
            }
        }

        readonly Func<FunctionExecutionContext, Task<IEndpointInstance>> endpointFactory;

        readonly SemaphoreSlim semaphoreLock = new SemaphoreSlim(initialCount: 1, maxCount: 1);
        ServiceBusTriggeredEndpointConfiguration configuration;

        PipelineInvoker pipeline;
        IEndpointInstance endpoint;
    }
}