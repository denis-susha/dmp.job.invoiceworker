namespace DMP.BL.Models.Bitcart;

/// <summary>Bitcart invoice statuses; names match Bitcart's wire values (parsed case-insensitively).</summary>
public enum InvoiceStatusBC
{
    Pending,
    Paid,
    Unconfirmed, // same as Paid; Electrum status
    Confirmed,
    Expired,
    Invalid,
    Complete,
    Refunded,
}
