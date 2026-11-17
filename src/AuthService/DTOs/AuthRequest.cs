using System.ComponentModel.DataAnnotations;

namespace NullBattle.Auth.DTOs;

public class AuthRequest
{
    [Required]
    [StringLength(50)]
    public string Nickname { get; set; } = "";

    [Required]
    [StringLength(72)]
    public string Password { get; set; } = "";
}