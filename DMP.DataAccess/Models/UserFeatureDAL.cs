namespace DMP.DataAccess.Models;

public class UserFeatureDAL
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public Dictionary<string, string>? Features { get; set; }
}
