using System.ComponentModel.DataAnnotations;
using NullBattle.TaskBank.Models;

namespace NullBattle.TaskBank.DTOs;

public class TaskRequest
{
    [Required]
    [StringLength(100)]
    public string Title { get; set; } = "";

    [Required]
    public string Description { get; set; } = "";

    [Range(1, long.MaxValue)]
    public long CategoryId { get; set; }

    [Range(1, 10000)]
    public int Points { get; set; }

    [Required]
    [StringLength(255)]
    public string Flag { get; set; } = "";

    [EnumDataType(typeof(Difficulty))]
    public Difficulty Difficulty { get; set; }
}