namespace NullBattle.Logs.DTOs;
public class LogResponse
{
    public long Id { get; set; }
    public string Service { get; set; } = "";
    public string Level { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}