namespace NServiceBus
{
    ///<summary>
    /// Assembly attribute to configure generated NServiceBus Azure Function.
    /// The attribute is used to wire up an auto-generated Service Bus trigger function, responding to messages in the queue specified by the name provided.
    ///</summary>
    [System.AttributeUsage(System.AttributeTargets.Assembly)]
    public sealed class NServiceBusTriggerFunctionAttribute : System.Attribute
    {
        /// <summary>
        /// Endpoint name that is the input queue name.
        /// </summary>
        public string EndpointName { get; }

        /// <summary>
        /// Override trigger function name.
        /// </summary>
        public string TriggerFunctionName { get; set; }

        /// <summary>
        /// Enable Azure Service Bus transactions to provide <see cref="TransportTransactionMode.SendsAtomicWithReceive"/> transport guarantees.
        /// </summary>
        public bool SendsAtomicWithReceive { get; set; }

        /// <summary>
        /// Gets or sets the app setting name that contains the Service Bus connection string.
        /// </summary>
        public string Connection { get; set; }

        /// <summary>
        /// Endpoint logical name.
        /// </summary>
        /// <param name="endpointName">Endpoint name that is the input queue name.</param>
        public NServiceBusTriggerFunctionAttribute(string endpointName)
        {
            EndpointName = endpointName;
        }
    }
}