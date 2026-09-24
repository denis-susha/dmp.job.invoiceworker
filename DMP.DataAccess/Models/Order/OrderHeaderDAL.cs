using DMP.DataAccess.Models.Enumerations;

namespace DMP.DataAccess.Models.Order;

public class OrderHeaderDAL
{
    public int OrderId { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public Currency Currency { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public OrderStatus Status { get; set; }
    /// <summary>Bitcart invoice id.</summary>
    public string? ReferenceNumber { get; set; }

    public virtual ICollection<OrderLineDAL>? OrderLines { get; set; }
}
