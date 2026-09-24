using DMP.DataAccess.Models.Enumerations;

namespace DMP.DataAccess.Models;

public class InvoiceWorkerTaskDAL
{
    public string InvoiceId { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public InvoiceWorkerTaskStatus Status { get; set; }
    public int OrderId { get; set; }
}
