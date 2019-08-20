namespace NServiceBus.AzureFunctions.AzureStorageQueues
{
    using System.Collections.Generic;

	/// <summary>
	/// Represents the Azure Storage Queues message within the NServiceBus pipeline.
	/// </summary>
    public class ASQMessageWrapper
    {
		/// <summary>
		/// TODO: What?
		/// </summary>
        public string IdForCorrelation { get; set; }

		/// <summary>
		/// The unique message Id for this message.
		/// </summary>
        public string Id { get; set; }

		/// <summary>
		/// If replying to this message, the address to send the reply to.
		/// </summary>
        public string ReplyToAddress { get; set; }

		/// <summary>
		/// A collection of headers used by infrastructure.
		/// </summary>
        public Dictionary<string, string> Headers { get; set; }

		/// <summary>
		/// Contains the message body represented as a serialized byte array.
		/// </summary>
        public byte[] Body { get; set; }

		/// <summary>
		/// The identifier that correlates response messages to the message that caused it.
		/// </summary>
        public string CorrelationId { get; set; }
    }
}