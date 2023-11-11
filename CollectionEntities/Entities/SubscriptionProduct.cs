namespace CollectionEntities.Entities
{
    public class SubscriptionProduct
    {
        public string SubscriptionName { get; set; }
        public string ProductId { get; set; }
        public decimal? Amount { get; set; }
        public string Icon { get; set; }
        public string Description { get; set; }
        public string PriceId { get; set; }
    }
}