using DMP.DataAccess.Models.Enumerations;

namespace DMP.DataAccess.Models;

public class EmailTemplateDAL
{
    public int EmailTemplateId { get; set; }
    /// <summary>Unique template identifier.</summary>
    public string Name { get; set; } = null!;
    public string Subject { get; set; } = null!;
    /// <summary>HTML with Razor syntax.</summary>
    public string Body { get; set; } = null!;
    public Language Language { get; set; }

    public virtual ICollection<MailDAL>? Mails { get; set; }
}
