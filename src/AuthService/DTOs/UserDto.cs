namespace NullBattle.Auth.DTOs;

public class UserDto
{
    public long Id { get; set; }
    public string Nickname { get; set; } = "";
    public List<string> Roles { get; set; } = new();
    public bool IsBanned { get; set; }
}