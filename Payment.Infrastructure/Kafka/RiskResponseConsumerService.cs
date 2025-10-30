using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Payment.Domain.Dtos;
using Payment.Infrastructure.Repositories;
using System.Text.Json;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Payment.Infrastructure.Kafka
{
    public class RiskResponseConsumerService : BackgroundService
    {
        private readonly ILogger<RiskResponseConsumerService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly string _bootstrapServers;
        private readonly string _topic;
        private readonly string _groupId;

        public RiskResponseConsumerService(ILogger<RiskResponseConsumerService> logger, IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
            _topic = configuration["Kafka:ResponseTopic"] ?? "risk-evaluation-response";
            _groupId = configuration["Kafka:ResponseGroupId"] ?? "payment-api-response-consumer";
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Inicio del Servicio de Respuesta al Riesgo para el Consumidor (topic: {Topic})", _topic);
            return Task.Run(() => ConsumeCiclo(stoppingToken), stoppingToken);
        }

        private void ConsumeCiclo(CancellationToken stoppingToken)
        {
            var conf = new ConsumerConfig
            {
                BootstrapServers = _bootstrapServers,
                GroupId = _groupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = true
            };

            using var consumer = new ConsumerBuilder<string, string>(conf).Build();
            consumer.Subscribe(_topic);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var cr = consumer.Consume(stoppingToken);
                        if (cr?.Message?.Value == null) continue;

                        _logger.LogInformation("Mensaje consumido de {Topic}: {Value}", _topic, cr.Message.Value);

                        RiskEvaluationResponseDto? dto = null;
                        try
                        {
                            dto = JsonSerializer.Deserialize<RiskEvaluationResponseDto>(cr.Message.Value);
                        }
                        catch (JsonException jex)
                        {
                            _logger.LogError(jex, "JSON no válido en el mensaje entrante: {Value}", cr.Message.Value);
                            continue;
                        }

                        if (dto == null) continue;

                        // mejor con scope para el repo
                        using var scope = _serviceProvider.CreateScope();
                        var repo = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();

                        var operationTask = repo.GetByExternalOperationIdAsync(dto.ExternalOperationId);
                        operationTask.Wait(stoppingToken);
                        var operation = operationTask.Result;

                        if (operation == null)
                        {
                            _logger.LogWarning("No se encontró ninguna operación para el ID de operación externa {ExternalId}", dto.ExternalOperationId);
                            continue;
                        }

                        operation.Status = dto.Status;
                        operation.UpdatedAt = DateTime.UtcNow;
                        repo.Update(operation);
                        var saveTask = repo.SaveChangesAsync();
                        saveTask.Wait(stoppingToken);
                        _logger.LogInformation("Operación actualizada {ExternalId} con estado {Status}", dto.ExternalOperationId, dto.Status);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (ConsumeException cex)
                    {
                        _logger.LogError(cex, "Excepción en el ciclo repetitivo del consumidor");
                        Task.Delay(TimeSpan.FromSeconds(1), stoppingToken).Wait(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Excepción inesperada en el ciclo repetitivo del consumidor");
                        Task.Delay(TimeSpan.FromSeconds(1), stoppingToken).Wait(stoppingToken);
                    }
                }
            }
            finally
            {
                try { consumer.Close(); } catch { }
            }
        }
    }
}
