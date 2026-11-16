namespace NullBattle.Auth.Models;

public class User
{
    public long Id { get; set; }
    public string Nickname { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public bool IsBanned { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<UserRole> UserRoles { get; set; } = new();
}