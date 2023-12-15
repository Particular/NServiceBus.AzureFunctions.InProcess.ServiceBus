# NServiceBus.AzureFunctions.InProcess.ServiceBus

NServiceBus.AzureFunctions.InProcess.ServiceBus supports processing messages in AzureFunctions using the Azure Service Bus trigger and the NServiceBus message pipeline.

It is part of the [Particular Service Platform](https://particular.net/service-platform), which includes [NServiceBus](https://particular.net/nservicebus) and tools to build, monitor, and debug distributed systems.

## Documentation

See the [Using NServiceBus in Azure Functions (in-process) documentation](https://docs.particular.net/samples/azure-functions/service-bus/) for more details on how to use it.

## Running tests locally

Requirements:

- Have the [Microsoft Azurite Storage Emulator](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite?tabs=visual-studio) running
- Configure an environment variable named `AzureWebJobsServiceBus` with an Azure Service Bus connection string
