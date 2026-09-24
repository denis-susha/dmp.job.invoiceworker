namespace DMP.DataAccess.Models;

public class FeatureDAL
{
    public int FeatureId { get; set; }
    public string Name { get; set; } = null!;

    public virtual ICollection<ProductFeatureDAL> ProductFeatures { get; set; } = null!;
}
