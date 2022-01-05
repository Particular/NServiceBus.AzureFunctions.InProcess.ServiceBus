namespace NServiceBus
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Runtime.Loader;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Transactions;
    using Azure.Messaging.ServiceBus;
    using AzureFunctions.InProcess.ServiceBus;
    using Extensibility;
    using Microsoft.Azure.WebJobs.ServiceBus;
    using Microsoft.Extensions.Logging;
    using NServiceBus.Logging;
    using Transport;
    using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

    class InProcessFunctionEndpoint : IFunctionEndpoint
    {
        public InProcessFunctionEndpoint(
            IStartableEndpointWithExternallyManagedContainer externallyManagedContainerEndpoint,
            ServiceBusTriggeredEndpointConfiguration configuration,
            IServiceProvider serviceProvider)
        {
            this.configuration = configuration;
            endpointFactory = _ => externallyManagedContainerEndpoint.Start(serviceProvider);
        }

        public async Task Send(object message, SendOptions options, ExecutionContext executionContext, ILogger functionsLogger = null)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionsLogger);

            await InitializeEndpointIfNecessary(executionContext, functionsLogger).ConfigureAwait(false);
            await endpoint.Send(message, options).ConfigureAwait(false);
        }

        public Task Send(object message, ExecutionContext executionContext, ILogger functionsLogger = null)
        {
            return Send(message, new SendOptions(), executionContext, functionsLogger);
        }

        public async Task Send<T>(Action<T> messageConstructor, SendOptions options, ExecutionContext executionContext, ILogger functionsLogger = null)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionsLogger);

            await InitializeEndpointIfNecessary(executionContext, functionsLogger).ConfigureAwait(false);
            await endpoint.Send(messageConstructor, options).ConfigureAwait(false);
        }

        public Task Send<T>(Action<T> messageConstructor, ExecutionContext executionContext, ILogger functionsLogger = null)
        {
            return Send(messageConstructor, new SendOptions(), executionContext, functionsLogger);
        }

        public async Task Publish(object message, PublishOptions options, ExecutionContext executionContext, ILogger functionsLogger = null)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionsLogger);

            await InitializeEndpointIfNecessary(executionContext, functionsLogger).ConfigureAwait(false);
            await endpoint.Publish(message, options).ConfigureAwait(false);
        }

        public Task Publish(object message, ExecutionContext executionContext, ILogger functionsLogger = null)
        {
            return Publish(message, new PublishOptions(), executionContext, functionsLogger);
        }

        public async Task Publish<T>(Action<T> messageConstructor, PublishOptions options, ExecutionContext executionContext, ILogger functionsLogger = null)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionsLogger);

            await InitializeEndpointIfNecessary(executionContext, functionsLogger).ConfigureAwait(false);
            await endpoint.Publish(messageConstructor, options).ConfigureAwait(false);
        }

        public Task Publish<T>(Action<T> messageConstructor, ExecutionContext executionContext, ILogger functionsLogger = null)
        {
            return Publish(messageConstructor, new PublishOptions(), executionContext, functionsLogger);
        }

        public async Task Subscribe(Type eventType, SubscribeOptions options, ExecutionContext executionContext, ILogger functionsLogger = null)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionsLogger);

            await InitializeEndpointIfNecessary(executionContext, functionsLogger).ConfigureAwait(false);
            await endpoint.Subscribe(eventType, options).ConfigureAwait(false);
        }

        public Task Subscribe(Type eventType, ExecutionContext executionContext, ILogger functionsLogger = null)
        {
            return Subscribe(eventType, new SubscribeOptions(), executionContext, functionsLogger);
        }

        public async Task Unsubscribe(Type eventType, UnsubscribeOptions options, ExecutionContext executionContext, ILogger functionsLogger = null)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionsLogger);

            await InitializeEndpointIfNecessary(executionContext, functionsLogger).ConfigureAwait(false);
            await endpoint.Unsubscribe(eventType, options).ConfigureAwait(false);
        }

        public Task Unsubscribe(Type eventType, ExecutionContext executionContext, ILogger functionsLogger = null)
        {
            return Unsubscribe(eventType, new UnsubscribeOptions(), executionContext, functionsLogger);
        }

        public async Task ProcessNonAtomic(
            ServiceBusReceivedMessage message,
            ExecutionContext executionContext,
            ILogger functionsLogger = null)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionsLogger);

            await InitializeEndpointIfNecessary(executionContext, functionsLogger)
                .ConfigureAwait(false);

            try
            {
                var messageContext = CreateMessageContext(message, new TransportTransaction());

                await pipeline.PushMessage(messageContext).ConfigureAwait(false);

            }
            catch (Exception exception)
            {
                var errorContext = CreateErrorContext(message, new TransportTransaction(), exception);

                var errorHandleResult = await pipeline.PushFailedMessage(errorContext).ConfigureAwait(false);

                if (errorHandleResult == ErrorHandleResult.Handled)
                {
                    return;
                }
                throw;
            }
        }

        public async Task ProcessAtomic(
            ServiceBusReceivedMessage message,
            ExecutionContext executionContext,
            ServiceBusClient serviceBusClient,
            ServiceBusMessageActions messageActions,
            ILogger functionsLogger = null)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionsLogger);

            try
            {
                await InitializeEndpointIfNecessary(executionContext, functionsLogger).ConfigureAwait(false);
            }
            catch (Exception)
            {
                await messageActions.AbandonMessageAsync(message).ConfigureAwait(false);
                throw;
            }

            try
            {
                using (var transaction = CreateTransaction())
                {
                    var transportTransaction = CreateTransportTransaction(message.PartitionKey, transaction, serviceBusClient);

                    var messageContext = CreateMessageContext(message, transportTransaction);

                    await pipeline.PushMessage(messageContext).ConfigureAwait(false);

                    await messageActions.SafeCompleteMessageAsync(message, transaction).ConfigureAwait(false);
                    transaction.Commit();
                }
            }
            catch (Exception exception)
            {
                ErrorHandleResult result;
                using (var transaction = CreateTransaction())
                {
                    var transportTransaction = CreateTransportTransaction(message.PartitionKey, transaction, serviceBusClient);

                    ErrorContext errorContext = CreateErrorContext(message, transportTransaction, exception);

                    result = await pipeline.PushFailedMessage(errorContext).ConfigureAwait(false);

                    if (result == ErrorHandleResult.Handled)
                    {
                        await messageActions.SafeCompleteMessageAsync(message, transaction).ConfigureAwait(false);
                    }

                    transaction.Commit();
                }

                if (result != ErrorHandleResult.Handled)
                {
                    await messageActions.AbandonMessageAsync(message).ConfigureAwait(false);
                }
            }
        }

        ErrorContext CreateErrorContext(ServiceBusReceivedMessage message, TransportTransaction transportTransaction, Exception exception)
        {
            var errorContext = new ErrorContext(
                exception,
                message.GetHeaders(),
                message.MessageId,
                message.Body.ToArray(),
                transportTransaction,
                message.DeliveryCount,
                new ContextBag());
            return errorContext;
        }

        MessageContext CreateMessageContext(ServiceBusReceivedMessage message, TransportTransaction transportTransaction)
        {
            var messageContext = new MessageContext(
                message.MessageId,
                message.GetHeaders(),
                message.Body.ToArray(),
                transportTransaction,
                new CancellationTokenSource(),
                new ContextBag());
            return messageContext;
        }

        static TransportTransaction CreateTransportTransaction(string messagePartitionKey, CommittableTransaction transaction, ServiceBusClient serviceBusClient)
        {
            var transportTransaction = new TransportTransaction();
            transportTransaction.Set(serviceBusClient);
            transportTransaction.Set("IncomingQueue.PartitionKey", messagePartitionKey);
            transportTransaction.Set(transaction);
            return transportTransaction;
        }

        static CommittableTransaction CreateTransaction() =>
            new CommittableTransaction(new TransactionOptions
            {
                IsolationLevel = IsolationLevel.Serializable,
                Timeout = TransactionManager.MaximumTimeout
            });

        internal async Task InitializeEndpointIfNecessary(ExecutionContext executionContext, ILogger logger, CancellationToken cancellationToken = default)
        {
            if (pipeline == null)
            {
                await semaphoreLock.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    if (pipeline == null)
                    {
                        var functionExecutionContext = new FunctionExecutionContext(executionContext, logger);
                        endpoint = await endpointFactory(functionExecutionContext).ConfigureAwait(false);

                        pipeline = configuration.PipelineInvoker;
                    }
                }
                finally
                {
                    semaphoreLock.Release();
                }
            }
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
                    LogManager.GetLogger<InProcessFunctionEndpoint>().DebugFormat(
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

        PipelineInvoker pipeline;
        IEndpointInstance endpoint;

        readonly Func<FunctionExecutionContext, Task<IEndpointInstance>> endpointFactory;
        readonly SemaphoreSlim semaphoreLock = new SemaphoreSlim(initialCount: 1, maxCount: 1);
        readonly ServiceBusTriggeredEndpointConfiguration configuration;
    }
}