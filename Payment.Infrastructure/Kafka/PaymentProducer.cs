using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Payment.Domain.Dtos;
using System.Text.Json;
using System.Threading;

namespace Payment.Infrastructure.Kafka
{
    public class PaymentProducer : IPaymentProducer, IDisposable
    {
        private readonly IProducer<string, string> _producer;
        private readonly string _topic;
        private readonly ILogger<PaymentProducer> _logger;

        public PaymentProducer(string bootstrapServers, string topic, ILogger<PaymentProducer> logger)
        {
            _topic = topic ?? throw new ArgumentNullException(nameof(topic));
            _logger = logger;
            var config = new ProducerConfig { BootstrapServers = bootstrapServers };
            _producer = new ProducerBuilder<string, string>(config).Build();
        }

        public async Task ProduceRiskRequestAsync(RiskEvaluationRequestDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var key = dto.ExternalOperationId.ToString();
                var value = JsonSerializer.Serialize(dto);
                var msg = new Message<string, string> { Key = key, Value = value };

                var result = await _producer.ProduceAsync(_topic, msg, cancellationToken);
                _logger.LogInformation("Mensaje generado en {Topic} partición {Partition} desplazamiento {Offset}", result.Topic, result.Partition, result.Offset);
            }
            catch (ProduceException<string, string> px)
            {
                _logger.LogError(px, "Se produjo una excepción al generar el mensaje para la operación externa con ID {ExternalId}.", dto.ExternalOperationId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Mensaje de error inesperado para la operación externa con ID {ExternalId}", dto.ExternalOperationId);
                throw;
            }
        }

        public void Dispose()
        {
            try
            {
                _producer?.Flush(TimeSpan.FromSeconds(5));
            }
            catch { }
            _producer?.Dispose();
        }
    }
}