> [!CAUTION] 
> **NServiceBus.AzureFunctions.InProcess.ServiceBus component is no longer supported**
>
> Microsoft announced that .NET 8 will be [the last release supporting the in-process hosting model](https://techcommunity.microsoft.com/t5/apps-on-azure-blog/net-on-azure-functions-august-2023-roadmap-update/ba-p/3910098) and that [beginning November 10, 2026, the in-process model for .NET apps in Azure Functions will no longer be supported](https://azure.microsoft.com/en-us/updates?id=retirement-support-for-the-inprocess-model-for-net-apps-in-azure-functions-ends-10-november-2026). This component will also stop receiving any updates.
> New projects should use the [isolated worker model](/nservicebus/hosting/azure-functions-service-bus/) instead. Follow [these instructions](/nservicebus/upgrades/azure-functions-service-bus-in-process-isolated-worker.md) to migrate.

# NServiceBus.AzureFunctions.InProcess.ServiceBus

NServiceBus.AzureFunctions.InProcess.ServiceBus supports processing messages in Azure Functions using the Azure Service Bus trigger and the NServiceBus message pipeline.

It is part of the [Particular Service Platform](https://particular.net/service-platform), which includes [NServiceBus](https://particular.net/nservicebus) and tools to build, monitor, and debug distributed systems.

## Migration

To prepare for migration, please review:

- [Migration instructions from InProcess to the Isolated model](https://docs.particular.net/nservicebus/upgrades/azure-functions-service-bus-in-process-isolated-worker).
- [Migration instructions from the old isolated package to the new multi-endpoint isolated package](https://docs.particular.net/nservicebus/upgrades/azure-functions-service-bus-isolated-to-azure-functions).

Review both migration guides and decide whether the safer approach is to first migrate from InProcess to the isolated package and then upgrade to the latest multi-endpoint package, or whether a direct migration from InProcess to the latest multi-endpoint package is acceptable.

## Documentation

See the [Azure Functions with Azure Service Bus (in-process) documentation](https://docs.particular.net/nservicebus/hosting/azure-functions-service-bus/in-process/) for more details on how to use it.

## Running tests locally

Requirements:

- Have the [Microsoft Azurite Storage Emulator](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite?tabs=visual-studio) running
- Configure an environment variable named `AzureWebJobsServiceBus` with an Azure Service Bus connection string
