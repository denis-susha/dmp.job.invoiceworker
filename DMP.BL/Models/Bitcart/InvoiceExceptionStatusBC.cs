namespace DMP.BL.Models.Bitcart;

/// <summary>Bitcart invoice exception statuses; names match Bitcart's wire values (parsed case-insensitively).</summary>
public enum InvoiceExceptionStatusBC : byte
{
    NONE,
    PAID_PARTIAL,
    PAID_OVER,
}
