namespace NullBattle.Logs.Dtos;

public class LogMessage
{
    public string Service { get; set; } = "";
    public string Level { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}