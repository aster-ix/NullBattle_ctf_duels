namespace NullBattle.Auth.Models;

public class UserRole
{
    public long UserId { get; set; }
    public long RoleId { get; set; }
    public DateTime AssignedAt { get; set; }

    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}