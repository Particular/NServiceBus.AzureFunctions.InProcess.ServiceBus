namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using AzureFunctions.InProcess.ServiceBus;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Logging;
using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

class InProcessFunctionEndpoint : IFunctionEndpoint
{
    public InProcessFunctionEndpoint(
        IStartableEndpointWithExternallyManagedContainer externallyManagedContainerEndpoint,
        ServerlessTransport serverlessTransport,
        IServiceProvider serviceProvider)
    {
        this.serverlessTransport = serverlessTransport;
        this.serverlessTransport.ServiceProvider = serviceProvider;
        endpointFactory = () => externallyManagedContainerEndpoint.Start(serviceProvider);
    }

    public async Task ProcessAtomic(
        ServiceBusReceivedMessage message,
        ExecutionContext executionContext,
        ServiceBusClient serviceBusClient,
        ServiceBusMessageActions messageActions,
        ILogger functionsLogger = null,
        CancellationToken cancellationToken = default)
    {
        FunctionsLoggerFactory.Instance.SetCurrentLogger(functionsLogger);

        try
        {
            await InitializeEndpointIfNecessary(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            await messageActions.AbandonMessageAsync(message, cancellationToken: cancellationToken).ConfigureAwait(false);
            throw;
        }

        await messageProcessor.ProcessAtomic(message, serviceBusClient, messageActions, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task ProcessNonAtomic(
        ServiceBusReceivedMessage message,
        ExecutionContext executionContext,
        ILogger functionsLogger = null,
        CancellationToken cancellationToken = default)
    {
        FunctionsLoggerFactory.Instance.SetCurrentLogger(functionsLogger);

        await InitializeEndpointIfNecessary(cancellationToken).ConfigureAwait(false);

        await messageProcessor.ProcessNonAtomic(message, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task Send(object message, SendOptions options, ExecutionContext executionContext, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
    {
        FunctionsLoggerFactory.Instance.SetCurrentLogger(functionsLogger);

        await InitializeEndpointIfNecessary(cancellationToken).ConfigureAwait(false);
        await endpoint.Send(message, options, cancellationToken).ConfigureAwait(false);
    }

    public Task Send(object message, ExecutionContext executionContext, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
    {
        return Send(message, new SendOptions(), executionContext, functionsLogger, cancellationToken);
    }

    public async Task Send<T>(Action<T> messageConstructor, SendOptions options, ExecutionContext executionContext, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
    {
        FunctionsLoggerFactory.Instance.SetCurrentLogger(functionsLogger);

        await InitializeEndpointIfNecessary(cancellationToken).ConfigureAwait(false);
        await endpoint.Send(messageConstructor, options, cancellationToken).ConfigureAwait(false);
    }

    public Task Send<T>(Action<T> messageConstructor, ExecutionContext executionContext, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
    {
        return Send(messageConstructor, new SendOptions(), executionContext, functionsLogger, cancellationToken);
    }

    public async Task Publish(object message, PublishOptions options, ExecutionContext executionContext, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
    {
        FunctionsLoggerFactory.Instance.SetCurrentLogger(functionsLogger);

        await InitializeEndpointIfNecessary(cancellationToken).ConfigureAwait(false);
        await endpoint.Publish(message, options, cancellationToken).ConfigureAwait(false);
    }

    public Task Publish(object message, ExecutionContext executionContext, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
    {
        return Publish(message, new PublishOptions(), executionContext, functionsLogger, cancellationToken);
    }

    public async Task Publish<T>(Action<T> messageConstructor, PublishOptions options, ExecutionContext executionContext, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
    {
        FunctionsLoggerFactory.Instance.SetCurrentLogger(functionsLogger);

        await InitializeEndpointIfNecessary(cancellationToken).ConfigureAwait(false);
        await endpoint.Publish(messageConstructor, options, cancellationToken).ConfigureAwait(false);
    }

    public Task Publish<T>(Action<T> messageConstructor, ExecutionContext executionContext, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
    {
        return Publish(messageConstructor, new PublishOptions(), executionContext, functionsLogger, cancellationToken);
    }

    public async Task Subscribe(Type eventType, SubscribeOptions options, ExecutionContext executionContext, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
    {
        FunctionsLoggerFactory.Instance.SetCurrentLogger(functionsLogger);

        await InitializeEndpointIfNecessary(cancellationToken).ConfigureAwait(false);
        await endpoint.Subscribe(eventType, options, cancellationToken).ConfigureAwait(false);
    }

    public Task Subscribe(Type eventType, ExecutionContext executionContext, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
    {
        return Subscribe(eventType, new SubscribeOptions(), executionContext, functionsLogger, cancellationToken);
    }

    public async Task Unsubscribe(Type eventType, UnsubscribeOptions options, ExecutionContext executionContext, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
    {
        FunctionsLoggerFactory.Instance.SetCurrentLogger(functionsLogger);

        await InitializeEndpointIfNecessary(cancellationToken).ConfigureAwait(false);
        await endpoint.Unsubscribe(eventType, options, cancellationToken).ConfigureAwait(false);
    }

    public Task Unsubscribe(Type eventType, ExecutionContext executionContext, ILogger functionsLogger = null, CancellationToken cancellationToken = default)
    {
        return Unsubscribe(eventType, new UnsubscribeOptions(), executionContext, functionsLogger, cancellationToken);
    }

    internal static readonly string[] AssembliesToExcludeFromScanning = {
        "Azure.Core.dll",
        "Azure.Identity.dll",
        "Azure.Security.KeyVault.Secrets.dll",
        "Azure.Storage.Blobs.dll",
        "Azure.Storage.Common.dll",
        "Google.Protobuf.dll",
        "Grpc.AspNetCore.Server.ClientFactory.dll",
        "Grpc.AspNetCore.Server.dll",
        "Grpc.Core.Api.dll",
        "Grpc.Net.Client.dll",
        "Grpc.Net.ClientFactory.dll",
        "Grpc.Net.Common.dll",
        "Microsoft.Extensions.Azure.dll",
        "Microsoft.Identity.Client.dll",
        "Microsoft.Identity.Client.Extensions.Msal.dll",
        "NCrontab.Signed.dll",
        "NServiceBus.Extensions.DependencyInjection.dll",
        "System.ClientModel.dll"
    };

    internal async Task InitializeEndpointIfNecessary(CancellationToken cancellationToken = default)
    {
        if (messageProcessor == null)
        {
            await semaphoreLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (messageProcessor == null)
                {
                    endpoint = await endpointFactory().ConfigureAwait(false);

                    messageProcessor = serverlessTransport.MessageProcessor;
                }
            }
            finally
            {
                semaphoreLock.Release();
            }
        }
    }

    IMessageProcessor messageProcessor;
    IEndpointInstance endpoint;

    readonly Func<Task<IEndpointInstance>> endpointFactory;
    readonly SemaphoreSlim semaphoreLock = new SemaphoreSlim(initialCount: 1, maxCount: 1);
    readonly ServerlessTransport serverlessTransport;
}