using NullBattle.Ratings.DTOs;
using NullBattle.Ratings.Exceptions;
using NullBattle.Ratings.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace NullBattle.Ratings.Messaging;

public class DuelEventConsumer : BackgroundService
{
    private const string QueueName = "duel.finished";
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DuelEventConsumer> _logger;
    private readonly ConnectionFactory _factory;

    public DuelEventConsumer(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<DuelEventConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMq:Host"]!,
            UserName = configuration["RabbitMq:User"]!,
            Password = configuration["RabbitMq:Password"]!
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ListenAsync(stoppingToken);
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Не удалось подключиться к RabbitMQ, повтор через 5 секунд");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ListenAsync(CancellationToken stoppingToken)
    {
        await using var connection = await _factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false,
            cancellationToken: stoppingToken);

        await channel.BasicQosAsync(0, 1, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (sender, args) => await HandleMessageAsync(channel, args);

        await channel.BasicConsumeAsync(QueueName, autoAck: false, consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation("RatingService слушает очередь {Queue}", QueueName);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleMessageAsync(IChannel channel, BasicDeliverEventArgs args)
    {
        try
        {
            var json = Encoding.UTF8.GetString(args.Body.ToArray());
            var message = JsonSerializer.Deserialize<DuelFinishedEvent>(json, JsonOptions);
            if (message == null)
            {
                throw new JsonException("Пустое сообщение");
            }

            using var scope = _scopeFactory.CreateScope();
            var ratingService = scope.ServiceProvider.GetRequiredService<RatingService>();
            await ratingService.ApplyDuelResultAsync(message);

            await channel.BasicAckAsync(args.DeliveryTag, false);
        }
        catch (Exception ex) when (ex is AppException || ex is JsonException)
        {
            _logger.LogError(ex, "Некорректное сообщение DuelFinished, оно отброшено");
            await channel.BasicAckAsync(args.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка обработки DuelFinished, сообщение вернётся в очередь");
            await Task.Delay(TimeSpan.FromSeconds(5));
            await channel.BasicNackAsync(args.DeliveryTag, false, true);
        }
    }
}