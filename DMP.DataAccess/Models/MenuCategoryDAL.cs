using System.Text.Json.Serialization;

namespace DMP.DataAccess.Models;

public class MenuCategoryDAL
{
    public int MenuCategoryId { get; set; }
    public short? MenuId { get; set; }
    public string Name { get; set; } = null!;
    public int? ParentId { get; set; }
    public int? Order { get; set; }
    public string Slug { get; set; } = null!;

    [JsonIgnore]
    public virtual MenuDAL? Menu { get; set; }
    [JsonIgnore]
    public virtual MenuCategoryDAL? Parent { get; set; }

    public virtual ICollection<MenuCategoryDAL>? Children { get; set; }
    [JsonIgnore]
    public virtual ICollection<ProductDAL>? Products { get; set; }
}
