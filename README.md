# NServiceBus.AzureFunctions.ServiceBus

Process messages in AzureFunctions using the Azure Service Bus trigger and the NServiceBus message pipeline.

## Running tests locally

Test projects included in the solution rely on two environment variables used by Azure Functions SDK. Those are `AzureWebJobsServiceBus` and `AzureWebJobsStorage`.
In order to run the tests, the values do not need to contain real connection strings. If you happened to have these environment variables, there's no need to modify the values.
