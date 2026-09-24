namespace DMP.BL.Models.Configurations;

public sealed class DmpWebApiOptions
{
    public const string Position = "DMP.API.WEB";

    public string Url { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
