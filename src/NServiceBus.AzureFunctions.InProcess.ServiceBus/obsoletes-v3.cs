namespace NServiceBus
{

    /// <summary>
    /// An NServiceBus endpoint hosted in Azure Function which does not receive messages automatically but only handles
    /// messages explicitly passed to it by the caller.
    /// </summary>
    [ObsoleteEx(ReplacementTypeOrMember = nameof(IFunctionEndpoint),
              TreatAsErrorFromVersion = "3",
              RemoveInVersion = "4")]
    public class FunctionEndpoint
    {
    }
}
