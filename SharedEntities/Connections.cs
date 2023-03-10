public class Connections
{
    public string SqlConnectionString { get; set; }
    public string PublicStorageConnectionString { get; set; }
    public string PrivateStorageConnectionString { get; set; }
    public string BatchServiceName { get; set; }
    public string BatchServiceEndpoint { get; set; }
    public string BatchServiceKey { get; set; }
    public string PoolName { get; set; }
    public string StripeSecretKey { get; set; }
    public string StripeWebhookKey { get; set; }
    public string Resolution { get; set; }
    public int MaxConcurrentActivityFunctions { get; set; }
    public TimeSpan FunctionTimeOut { get; set; }
}