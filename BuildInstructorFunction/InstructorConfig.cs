namespace BuildInstructorFunction
{
    public class InstructorConfig
    {
        public string StripeWebhookKey { get; set; }
        public string BatchServiceEndpoint { get; set; }
        public string BatchServiceName { get; set; }
        public string BatchServiceKey { get; set; }
        public string PoolName { get; set; }
        public string AZURE_CLIENT_ID { get; set; }
        public string FreeBuilderQueueName { get; set; }
    }
}
