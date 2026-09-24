using DMP.BL.Models;

namespace DMP.BL.Services;

public interface IPaymentService
{
    /// <summary>
    /// Applies a Bitcart invoice update to the order, records the payment, notifies DMP.API.WEB and,
    /// when the payment is complete, queues the purchase-complete email.
    /// </summary>
    Task<PaymentUpdateResult> ProcessPaymentUpdateMessage(WsPaymentUpdateMessage message, string invoiceId, int orderId);
}
