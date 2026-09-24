namespace DMP.DataAccess.Models;

public class MenuDAL
{
    public short MenuId { get; set; }
    public string Description { get; set; } = null!;

    public virtual ICollection<MenuCategoryDAL>? Categories { get; set; }
}
