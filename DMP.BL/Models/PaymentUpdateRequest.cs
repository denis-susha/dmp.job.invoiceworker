using DMP.BL.Models.Enumerations;

namespace DMP.BL.Models;

/// <summary>Body of <c>POST messages/workernotification/paymentupdate</c> sent to DMP.API.WEB.</summary>
public sealed class PaymentUpdateRequest
{
    public Guid UserId { get; init; }
    public PaymentStatus Status { get; init; }
    public decimal SentAmount { get; init; }
    public int OrderId { get; init; }
}
