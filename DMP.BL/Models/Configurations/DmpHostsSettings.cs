namespace DMP.BL.Models.Configurations;

public sealed class DmpHostsSettings
{
    public const string Position = "DmpHosts";

    public string Client { get; set; } = string.Empty;
    public string Seller { get; set; } = string.Empty;
}
