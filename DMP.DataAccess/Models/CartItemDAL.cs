namespace DMP.DataAccess.Models;

public class CartItemDAL
{
    public Guid UserId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public bool Selected { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
