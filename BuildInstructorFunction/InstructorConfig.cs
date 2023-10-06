namespace BuildInstructorFunction
{
    public class InstructorConfig
    {
        public string StripeWebhookKey { get; set; }
        public string BatchServiceEndpoint { get; set; }
        public string BatchServiceName { get; set; }
        public string BatchServiceKey { get; set; }
        public string PoolName { get; set; }
        public string ManagedIdentityIdReference { get; set; }
        public string FreeBuilderQueueName { get; set; }
        public string NewVideoQueueName { get; set; }
        public string PrivateAccountName { get; set; }
    }
}
