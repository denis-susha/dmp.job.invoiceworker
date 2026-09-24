namespace DMP.BL.Models.Mail;

/// <summary>Model of the <c>ClientPurchaseComplete</c> email template.</summary>
public sealed class ClientPurchaseCompleteMail
{
    public required string Url { get; init; }
    public required string LogoUrl { get; init; }
    public required string Year { get; init; }
}
