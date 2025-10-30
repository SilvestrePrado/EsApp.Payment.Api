using Confluent.Kafka;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace RiskSimulator
{
    public record RiskEvaluationRequestDto(Guid ExternalOperationId, Guid CustomerId, decimal Amount);
    public record RiskEvaluationResponseDto(Guid ExternalOperationId, string Status);

    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var bootstrap = args.Length > 0 ? args[0] : "localhost:9092";
            var requestTopic = "risk-evaluation-request";
            var responseTopic = "risk-evaluation-response";

            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = bootstrap,
                GroupId = "risk-simulator-group",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            var producerConfig = new ProducerConfig
            {
                BootstrapServers = bootstrap
            };

            // diccionario en memoria, se reiniciara al reiniciar el proceso.
            var dailyAcc = new Dictionary<Guid, decimal>();

            using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
            using var producer = new ProducerBuilder<string, string>(producerConfig).Build();

            consumer.Subscribe(requestTopic);
            Console.WriteLine("Simulador de Riesgos iniciado. Escuchando peticiones...");

            while (true)
            {
                try
                {
                    var cr = consumer.Consume();
                    if (cr?.Message?.Value == null) continue;

                    RiskEvaluationRequestDto? req = null;
                    try
                    {
                        req = JsonSerializer.Deserialize<RiskEvaluationRequestDto>(cr.Message.Value);
                    }
                    catch (JsonException)
                    {
                        Console.WriteLine("JSON inválido en la solicitud: " + cr.Message.Value);
                        continue;
                    }

                    if (req is null) continue;

                    string decision;
                    if (req.Amount > 2000m)
                    {
                        decision = "denied";
                    }
                    else
                    {
                        dailyAcc.TryGetValue(req.CustomerId, out var acc);
                        if (acc + req.Amount > 5000m)
                        {
                            decision = "denied";
                        }
                        else
                        {
                            decision = "accepted";
                            dailyAcc[req.CustomerId] = acc + req.Amount;
                        }
                    }

                    var resp = new RiskEvaluationResponseDto(req.ExternalOperationId, decision);
                    var value = JsonSerializer.Serialize(resp);
                    await producer.ProduceAsync(responseTopic, new Message<string, string> { Key = req.ExternalOperationId.ToString(), Value = value });
                    Console.WriteLine($"Se procesó {req.ExternalOperationId} para {req.CustomerId} - {req.Amount} => {decision}");
                }
                catch (ConsumeException cex)
                {
                    Console.WriteLine($"Consume excepcion: {cex.Error.Reason}");
                    await Task.Delay(1000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error inesperado: {ex}");
                    await Task.Delay(1000);
                }
            }
        }
    }
}
