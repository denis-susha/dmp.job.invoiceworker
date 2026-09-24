namespace DMP.BL.Models.Configurations;

public sealed class BitcartOptions
{
    public const string Position = "BitcartOptions";

    /// <summary>Base URI of the Bitcart invoice WebSocket; the invoice id is appended as the last segment.</summary>
    public string WebSocketUri { get; set; } = string.Empty;
}
