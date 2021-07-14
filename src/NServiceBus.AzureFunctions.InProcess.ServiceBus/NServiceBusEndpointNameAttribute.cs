namespace NServiceBus
{
    ///<summary>
    /// Assembly attribute to specify NServiceBus logical endpoint name.
    /// This name is used to wire up an auto-generated service bus trigger function, responding to messages in the queue specified by the name provided.
    ///</summary>
    [System.AttributeUsage(System.AttributeTargets.Assembly)]
    public sealed class NServiceBusEndpointNameAttribute : System.Attribute
    {
        /// <summary>
        /// Endpoint name that is the input queue name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Override trigger function name. By default the value is "NServiceBusFunctionEndpointTrigger-{Nme}" where Name is the endpoint name.
        /// </summary>
        public string TriggerFunctionName { get; }

        ///<summary>
        ///Endpoint logical name.
        ///</summary>
        ///<param name="name">Endpoint name that is the input queue name.</param>
        ///<param name="triggerFunctionName">Name given to the auto-generated trigger function.</param>
        public NServiceBusEndpointNameAttribute(string name, string triggerFunctionName = default)
        {
            Name = name;
            TriggerFunctionName = triggerFunctionName;
        }
    }
}