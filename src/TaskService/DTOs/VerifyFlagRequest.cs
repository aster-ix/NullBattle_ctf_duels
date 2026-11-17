using System.ComponentModel.DataAnnotations;

namespace NullBattle.TaskBank.DTOs;

public class VerifyFlagRequest
{
    [Required]
    [StringLength(255)]
    public string Flag { get; set; } = "";
}