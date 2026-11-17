using NullBattle.Duels.DTOs;
using RabbitMQ.Client;
using System.Text.Json;

namespace NullBattle.Duels.Messaging;

public class DuelEventPublisher
{
    private const string QueueName = "duel.finished";
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

    private readonly ConnectionFactory _factory;
    private readonly ILogger<DuelEventPublisher> _logger;

    public DuelEventPublisher(IConfiguration configuration, ILogger<DuelEventPublisher> logger)
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

    public async Task PublishDuelFinishedAsync(DuelFinishedEvent message)
    {
        try
        {
            await using var connection = await _factory.CreateConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false);

            var body = JsonSerializer.SerializeToUtf8Bytes(message, JsonOptions);
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
            _logger.LogError(ex, "Не удалось отправить DuelFinished для дуэли {DuelId}", message.DuelId);
        }
    }
}