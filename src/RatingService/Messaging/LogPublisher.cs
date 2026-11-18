using RabbitMQ.Client;
using System.Text.Json;

namespace NullBattle.Ratings.Messaging;

public class LogPublisher
{
    private const string ServiceName = "RatingService";
    private const string QueueName = "logs";
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

    private readonly ConnectionFactory _factory;
    private readonly ILogger<LogPublisher> _logger;

    public LogPublisher(IConfiguration configuration, ILogger<LogPublisher> logger)
    {
        _factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMq:Host"]!,
            UserName = configuration["RabbitMq:User"]!,
            Password = configuration["RabbitMq:Password"]!,
            RequestedConnectionTimeout = TimeSpan.FromSeconds(5)
        };
        _logger = logger;
    }

    public void Info(string message)
    {
        Send("INFO", message);
    }

    public void Warn(string message)
    {
        Send("WARN", message);
    }

    public void Error(string message)
    {
        Send("ERROR", message);
    }

    private void Send(string level, string message)
    {
        var record = new
        {
            service = ServiceName,
            level,
            message,
            createdAt = DateTime.UtcNow
        };
        _ = PublishAsync(record);
    }

    private async Task PublishAsync(object record)
    {
        try
        {
            await using var connection = await _factory.CreateConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false);

            var body = JsonSerializer.SerializeToUtf8Bytes(record, JsonOptions);
            var properties = new BasicProperties { Persistent = true };

            await channel.BasicPublishAsync(
                exchange: "",
                routingKey: QueueName,
                mandatory: false,
                basicProperties: properties,
                body: body);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось отправить запись в журнал");
        }
    }
}