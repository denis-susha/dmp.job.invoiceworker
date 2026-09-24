using DMP.DataAccess.Models.Enumerations;

namespace DMP.DataAccess.Models;

public class UserDAL
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public byte[] Salt { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public UserStatus Status { get; set; }
    public UserFlags Flags { get; set; }
    public Language Language { get; set; }
}
