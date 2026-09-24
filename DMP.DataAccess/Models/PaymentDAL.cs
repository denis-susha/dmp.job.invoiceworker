namespace DMP.DataAccess.Models;

public class PaymentDAL
{
    public int PaymentId { get; set; }
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public string? CurrencyCode { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
