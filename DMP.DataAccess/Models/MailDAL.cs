using DMP.DataAccess.Models.Enumerations;

namespace DMP.DataAccess.Models;

public class MailDAL
{
    public long MailId { get; set; }
    public string To { get; set; } = null!;
    public string From { get; set; } = null!;
    public string? Subject { get; set; }
    public string? Copy { get; set; }
    public string? Body { get; set; }
    public MailStatus Status { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public int Attempts { get; set; } = 0;
    public int? EmailTemplateId { get; set; }
    public string? Model { get; set; }

    public virtual EmailTemplateDAL? EmailTemplate { get; set; }
}
