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

        ///<summary>
        ///Endpoint logical name.
        ///</summary>
        ///<param name="name">Endpoint name that is the input queue name.</param>
        public NServiceBusEndpointNameAttribute(string name) => Name = name;
    }
}