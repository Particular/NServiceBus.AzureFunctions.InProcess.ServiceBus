namespace NServiceBus
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Runtime.Loader;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Transactions;
    using AzureFunctions.InProcess.ServiceBus;
    using Extensibility;
    using Logging;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.ServiceBus.Core;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging;
    using Transport;
    using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

    /// <summary>
    /// An NServiceBus endpoint hosted in Azure Function which does not receive messages automatically but only handles
    /// messages explicitly passed to it by the caller.
    /// </summary>
    public class FunctionEndpoint : IFunctionEndpoint
    {
        // This ctor is used for the FunctionsHostBuilder scenario where the endpoint is created already during configuration time using the function host's container.
        internal FunctionEndpoint(IStartableEndpointWithExternallyManagedContainer externallyManagedContainerEndpoint,
            ServiceBusTriggeredEndpointConfiguration configuration, IServiceProvider serviceProvider)
        {
            this.configuration = configuration;
            endpointFactory = _ => externallyManagedContainerEndpoint.Start(serviceProvider);
        }

        /// <summary>
        /// TODO.
        /// </summary>
        public Task AutoDetectProcess(Message message, ExecutionContext executionContext, IMessageReceiver messageReceiver, ILogger functionsLogger = null)
        {
            var st = new StackTrace();
            var frames = st.GetFrames();
            foreach (var frame in frames)
            {
                var method = frame.GetMethod();
                if (method?.GetCustomAttribute<FunctionNameAttribute>() != null)
                {
                    foreach (var parameter in method.GetParameters())
                    {
                        ServiceBusTriggerAttribute serviceBusTriggerAttribute;
                        if (parameter.ParameterType == typeof(Message)
                            && (serviceBusTriggerAttribute = parameter.GetCustomAttribute<ServiceBusTriggerAttribute>()) != null)
                        {
                            if (serviceBusTriggerAttribute.AutoComplete)
                            {
                                // Autocomplete enabled -> no transactions
                                return Process(message, executionContext, functionsLogger);
                            }
                            else
                            {
                                // Autocomplete disabled -> transactions
                                return ProcessTransactional(message, executionContext, messageReceiver, functionsLogger);
                            }
                        }
                    }
                }
            }

            throw new Exception($"Could not locate {nameof(ServiceBusTriggerAttribute)} to infer AutoComplete setting.");
        }

        /// <summary>
        /// Processes a message received from an AzureServiceBus trigger using the NServiceBus message pipeline. All messages are committed transactionally with the successful processing of the incoming message.
        /// </summary>
        public async Task ProcessTransactional(Message message, ExecutionContext executionContext, IMessageReceiver messageReceiver, ILogger functionsLogger = null)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionsLogger);

            var functionExecutionContext = new FunctionExecutionContext(executionContext, functionsLogger);

            try
            {
                await InitializeEndpointIfNecessary(functionExecutionContext, CancellationToken.None)
                    .ConfigureAwait(false);

                await Process(message,
                        tx => messageReceiver.SafeCompleteAsync(message, tx),
                        () => CreateTransaction(),
                        tx => CreateTransportTransaction(tx),
                        pipeline)
                    .ConfigureAwait(false);
            }
            catch (Exception)
            {
                // abandon message outside of a transaction scope to ensure the abandon operation can't be rolled back
                await messageReceiver.AbandonAsync(message.SystemProperties.LockToken).ConfigureAwait(false);
                throw;
            }

            CommittableTransaction CreateTransaction() =>
                new CommittableTransaction(new TransactionOptions
                {
                    IsolationLevel = IsolationLevel.Serializable,
                    Timeout = TransactionManager.MaximumTimeout
                });

            TransportTransaction CreateTransportTransaction(CommittableTransaction transaction)
            {
                var transportTransaction = new TransportTransaction();
                transportTransaction.Set((messageReceiver.ServiceBusConnection, messageReceiver.Path));
                transportTransaction.Set("IncomingQueue.PartitionKey", message.PartitionKey);
                transportTransaction.Set(transaction);
                return transportTransaction;
            }
        }

        /// <summary>
        /// Processes a message received from an AzureServiceBus trigger using the NServiceBus message pipeline.
        /// </summary>
        public async Task Process(Message message, ExecutionContext executionContext, ILogger functionsLogger = null)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionsLogger);

            var functionExecutionContext = new FunctionExecutionContext(executionContext, functionsLogger);

            await InitializeEndpointIfNecessary(functionExecutionContext, CancellationToken.None)
                .ConfigureAwait(false);

            await Process(message,
                    _ => Task.CompletedTask,
                    () => null,
                    _ => new TransportTransaction(),
                    pipeline)
                .ConfigureAwait(false);
        }

        internal static async Task Process(Message message, Func<CommittableTransaction, Task> onComplete, Func<CommittableTransaction> transactionFactory, Func<CommittableTransaction, TransportTransaction> transportTransactionFactory, PipelineInvoker pipeline)
        {
            var messageId = message.GetMessageId();

            try
            {
                using (var transaction = transactionFactory())
                {
                    var transportTransaction = transportTransactionFactory(transaction);
                    var messageContext = CreateMessageContext(transportTransaction);

                    await pipeline.PushMessage(messageContext).ConfigureAwait(false);

                    await onComplete(transaction).ConfigureAwait(false);

                    transaction?.Commit();
                }
            }
            catch (Exception exception)
            {
                using (var transaction = transactionFactory())
                {
                    var transportTransaction = transportTransactionFactory(transaction);
                    var errorContext = new ErrorContext(
                        exception,
                        message.GetHeaders(),
                        messageId,
                        message.Body,
                        transportTransaction,
                        message.SystemProperties.DeliveryCount);

                    var errorHandleResult = await pipeline.PushFailedMessage(errorContext).ConfigureAwait(false);

                    if (errorHandleResult == ErrorHandleResult.Handled)
                    {
                        await onComplete(transaction).ConfigureAwait(false);

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
                    new CancellationTokenSource(),
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