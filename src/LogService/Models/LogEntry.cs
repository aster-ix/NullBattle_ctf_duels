using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Expressions.Internal;

namespace NullBattle.Logs.Models;


public class LogEntry
{
    public long Id { get; set; }
    public string Service { get; set; } = "";
    public string Level { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}