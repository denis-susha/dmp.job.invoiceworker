namespace DMP.DataAccess.Models.Enumerations;

public enum OrderStatus : byte
{
    New = 0,
    Paid = 2,
    Complete = 5,
    PaidPartial = 7,
    PaidOver = 8,
    Expired = 10,
    Refunded = 20,
    ErrorOnCreation = 30,
    PaidError = 31,
}
