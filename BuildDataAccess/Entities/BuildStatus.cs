namespace SharedEntities.Models
{
    public enum BuildStatus
    {
        PaymentAuthorisationPending = 1,
        BuildingPending = 2,
        PaymentChargePending = 3,
        Complete = 4,
        Failed = 5
    }
}