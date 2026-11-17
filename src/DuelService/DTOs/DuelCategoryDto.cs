namespace NullBattle.Duels.DTOs;

public class DuelCategoryDto
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public long? BannedByUserId { get; set; }
}