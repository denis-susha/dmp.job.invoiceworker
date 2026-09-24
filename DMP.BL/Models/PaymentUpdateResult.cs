using DMP.DataAccess.Models.Enumerations;

namespace DMP.BL.Models;

/// <param name="OrderStatus">The order status the update was mapped to.</param>
/// <param name="IsInvoiceFinal">
/// Whether Bitcart will not change the invoice any more (complete, expired or invalid).
/// The order status alone does not tell this: an overpaid invoice is reported as PaidOver
/// while it is still only paid and waiting for confirmations.
/// </param>
public readonly record struct PaymentUpdateResult(OrderStatus OrderStatus, bool IsInvoiceFinal);
