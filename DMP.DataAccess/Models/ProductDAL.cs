namespace DMP.DataAccess.Models;

public class ProductDAL
{
    public int ProductId { get; set; }
    public int MenuCategoryId { get; set; }
    public UserFeatureDAL? UserFeature { get; set; }
    public string Slug { get; set; } = null!;
    public Guid SellerId { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public bool Unlimited { get; set; }
    public string[] ImgLinks { get; set; } = null!;

    public virtual MenuCategoryDAL? MenuCategory { get; set; }
    public virtual ICollection<ProductFeatureDAL>? ProductFeatures { get; set; }
}
