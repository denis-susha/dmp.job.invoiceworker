using DMP.DataAccess.Models.Enumerations;

namespace DMP.DataAccess.Models.Order;

public class OrderLineDAL
{
    public int OrderLineId { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public Guid SellerId { get; set; }
    public decimal Price { get; set; }
    public Currency Currency { get; set; }
    public int Quantity { get; set; }

    public virtual OrderHeaderDAL? OrderHeader { get; set; }
}
