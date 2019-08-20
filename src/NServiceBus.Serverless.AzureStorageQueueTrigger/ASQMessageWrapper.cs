namespace AzureFunctionsDemo
{
    using System.Collections.Generic;

    public class ASQMessageWrapper
    {
        public string IdForCorrelation { get; set; }

        public string Id { get; set; }

        public string ReplyToAddress { get; set; }

        public Dictionary<string, string> Headers { get; set; }

        public byte[] Body { get; set; }

        public string CorrelationId { get; set; }
    }
}