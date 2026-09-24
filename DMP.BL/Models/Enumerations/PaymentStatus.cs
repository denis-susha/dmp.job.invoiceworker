namespace DMP.BL.Models.Enumerations;

public enum PaymentStatus
{
    InProgress = 0,
    Complete = 1,
    PaidPartial = 2,
    Expired = 3,
    Refunded = 4,
    PaidError = 5,
}
