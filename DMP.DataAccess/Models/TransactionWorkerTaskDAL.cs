using DMP.DataAccess.Models.Enumerations;

namespace DMP.DataAccess.Models;

public class TransactionWorkerTaskDAL
{
    public string InvoiceId { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public TransactionWorkerTaskStatus Status { get; set; }
    public int OrderId { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public int Attempts { get; set; }
    public string? SourceTransactionId { get; set; }
    public TransactionWorkerTaskType Type { get; set; }
    public Guid UserId { get; set; }
    public string? Comment { get; set; }
}
