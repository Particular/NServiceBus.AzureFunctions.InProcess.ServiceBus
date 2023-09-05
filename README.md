# NServiceBus.AzureFunctions.InProcess.ServiceBus

Process messages in AzureFunctions using the Azure Service Bus trigger and the NServiceBus message pipeline.

## Running tests locally

Requirements:

- Have the [Microsoft Azurite Storage Emulator](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite?tabs=visual-studio) running
- Configure an environment variable named `AzureWebJobsServiceBus` with an Azure Service Bus connection string
