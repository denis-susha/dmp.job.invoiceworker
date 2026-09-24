namespace DMP.DataAccess.Models;

public class TranslationDAL
{
    public string Key { get; set; } = null!;
    public Dictionary<string, string> Translations { get; set; } = null!;
}
