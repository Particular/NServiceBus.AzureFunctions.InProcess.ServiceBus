namespace NServiceBus
{
    ///<summary>
    /// Assembly attribute to specify NServiceBus logical endpoint name.
    /// This name is used to wire up an auto-generated service bus trigger function, responding to messages in the queue specified by the name provided.
    ///</summary>
    [ObsoleteEx(
        ReplacementTypeOrMember = nameof(NServiceBusTriggerFunctionAttribute),
        TreatAsErrorFromVersion = "2",
        RemoveInVersion = "3")]
    public sealed class NServiceBusEndpointNameAttribute : System.Attribute
    {
    }
}