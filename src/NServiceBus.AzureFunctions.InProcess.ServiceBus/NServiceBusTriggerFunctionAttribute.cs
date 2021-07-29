namespace NServiceBus
{
    ///<summary>
    /// Assembly attribute to specify NServiceBus logical endpoint name.
    /// The attribute is used to wire up an auto-generated Service Bus trigger function, responding to messages in the queue specified by the name provided.
    ///</summary>
    [System.AttributeUsage(System.AttributeTargets.Assembly)]
    public sealed class NServiceBusTriggerFunctionAttribute : System.Attribute
    {
        /// <summary>
        /// Endpoint name that is the input queue name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Override trigger function name.
        /// </summary>
        public string TriggerFunctionName { get; set; }

        /// <summary>
        /// Enable cross-entity transactions.
        /// </summary>
        public bool EnableCrossEntityTransactions { get; set; }

        /// <summary>
        /// Endpoint logical name.
        /// </summary>
        /// <param name="name">Endpoint name that is the input queue name.</param>
        public NServiceBusTriggerFunctionAttribute(string name)
        {
            Name = name;
        }
    }
}