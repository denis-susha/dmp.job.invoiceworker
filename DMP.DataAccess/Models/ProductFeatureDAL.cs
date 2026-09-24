namespace DMP.DataAccess.Models;

public class ProductFeatureDAL
{
    public int ProductId { get; set; }
    public int FeatureId { get; set; }
    public string Value { get; set; } = null!;

    public virtual ProductDAL? Product { get; set; }
    public virtual FeatureDAL? Feature { get; set; }
}
