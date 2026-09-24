using System.Text.Json.Serialization;

namespace DMP.BL.Models;

/// <summary>Invoice update pushed by the Bitcart <c>/ws/invoices/{id}</c> WebSocket.</summary>
public sealed class WsPaymentUpdateMessage
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = null!;

    [JsonPropertyName("exception_status")]
    public string ExceptionStatus { get; set; } = null!;

    [JsonPropertyName("sent_amount")]
    public string SentAmount { get; set; } = null!;

    [JsonPropertyName("paid_currency")]
    public string? PaidCurrency { get; set; }
}
