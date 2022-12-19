namespace NServiceBus.AzureFunctions.InProcess.ServiceBus.Serverless;

class FunctionInvocationMode
{
    public FunctionInvocationMode(bool atomic)
    {
        Atomic = atomic;
    }

    public bool Atomic { get; }
}