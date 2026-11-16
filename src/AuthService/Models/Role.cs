namespace NullBattle.Auth.Models;

public class Role
{
    public long Id { get; set; }
    public string Name { get; set; } = "";

    public List<UserRole> UserRoles { get; set; } = new();
}