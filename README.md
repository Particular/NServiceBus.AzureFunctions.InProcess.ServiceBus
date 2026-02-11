> [!CAUTION] 
> **NServiceBus.AzureFunctions.InProcess.ServiceBus has been officially sunsetted.**  
> Microsoft announced that .NET 8 will be [the last release supporting the in-process hosting model](https://techcommunity.microsoft.com/t5/apps-on-azure-blog/net-on-azure-functions-august-2023-roadmap-update/ba-p/3910098).
> We will continue to provide support and address critical fixes during the sunset period, but no new features will be added.  
> New projects should use the [isolated worker model](https://docs.particular.net/nservicebus/hosting/azure-functions-service-bus/) instead. Follow [these instructions](https://docs.particular.net/nservicebus/upgrades/azure-functions-service-bus-in-process-isolated-worker) to migrate.

# NServiceBus.AzureFunctions.InProcess.ServiceBus

NServiceBus.AzureFunctions.InProcess.ServiceBus supports processing messages in AzureFunctions using the Azure Service Bus trigger and the NServiceBus message pipeline.

It is part of the [Particular Service Platform](https://particular.net/service-platform), which includes [NServiceBus](https://particular.net/nservicebus) and tools to build, monitor, and debug distributed systems.

## Documentation

See the [Azure Functions with Azure Service Bus (in-process) documentation](https://docs.particular.net/nservicebus/hosting/azure-functions-service-bus/in-process/) for more details on how to use it.

## Running tests locally

Requirements:

- Have the [Microsoft Azurite Storage Emulator](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite?tabs=visual-studio) running
- Configure an environment variable named `AzureWebJobsServiceBus` with an Azure Service Bus connection string
