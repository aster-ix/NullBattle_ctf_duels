using System.Text;
using System.Text.Json;
using NullBattle.Logs.Dtos;
using NullBattle.Logs.Exceptions;
using NullBattle.Logs.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NullBattle.Logs.Messaging;

public class LogConsumer : BackgroundService
{
    private const string QueueName = "logs";
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LogConsumer> _logger;
    private readonly ConnectionFactory _factory;

    public LogConsumer(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<LogConsumer> logger)
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
        await channel.BasicQosAsync(0, 10, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (sender, args) => await HandleMessageAsync(channel, args);

        await channel.BasicConsumeAsync(QueueName, autoAck: false, consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation("LogService слушает очередь {Queue}", QueueName);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleMessageAsync(IChannel channel, BasicDeliverEventArgs args)
    {
        try
        {
            var json = Encoding.UTF8.GetString(args.Body.ToArray());
            var message = JsonSerializer.Deserialize<LogMessage>(json, JsonOptions);
            if (message == null)
            {
                throw new JsonException("Пустое сообщение");
            }

            using var scope = _scopeFactory.CreateScope();
            var logService = scope.ServiceProvider.GetRequiredService<LogService>();
            await logService.SaveAsync(message);

            await channel.BasicAckAsync(args.DeliveryTag, false);
        }
        catch (Exception ex) when (ex is AppException || ex is JsonException)
        {
            _logger.LogError(ex, "Некорректная запись журнала, она отброшена");
            await channel.BasicAckAsync(args.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            // Временная ошибка 
            _logger.LogError(ex, "Ошибка сохранения записи, она вернётся в очередь");
            await Task.Delay(TimeSpan.FromSeconds(5));
            await channel.BasicNackAsync(args.DeliveryTag, false, true);
        }
    }
}