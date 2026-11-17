using System.ComponentModel.DataAnnotations;
using NullBattle.Duels.Models;

namespace NullBattle.Duels.DTOs;

public class CreateDuelRequest
{
    [Range(1, long.MaxValue)]
    public long Player1Id { get; set; }

    [Range(1, long.MaxValue)]
    public long Player2Id { get; set; }

    [EnumDataType(typeof(Difficulty))]
    public Difficulty Difficulty { get; set; }
}