using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Payment.Domain.Dtos;
using Payment.Domain.Entities;
using Payment.Infrastructure.Kafka;
using Payment.Infrastructure.Repositories;
using System;
using System.Threading.Tasks;

namespace Payment.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentRepository _repo;
        private readonly IPaymentProducer _producer;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(IPaymentRepository repo, IPaymentProducer producer, ILogger<PaymentsController> logger)
        {
            _repo = repo;
            _producer = producer;
            _logger = logger;
        }

        public record CreatePaymentRequest(Guid CustomerId, Guid ServiceProviderId, int PaymentMethodId, decimal Amount);

        [HttpPost]
        public async Task<IActionResult> Create(CreatePaymentRequest request)
        {
            try
            {
                var externalId = Guid.NewGuid();

                var op = new PaymentOperation
                {
                    ExternalOperationId = externalId,
                    CustomerId = request.CustomerId,
                    ServiceProviderId = request.ServiceProviderId,
                    PaymentMethodId = request.PaymentMethodId,
                    Amount = request.Amount,
                    Status = "evaluating",
                    CreatedAt = DateTime.UtcNow
                };

                await _repo.AddAsync(op);
                await _repo.SaveChangesAsync();

                // Aca el Produce para Kafka
                var dto = new RiskEvaluationRequestDto(externalId, request.CustomerId, request.Amount);
                await _producer.ProduceRiskRequestAsync(dto);

                return CreatedAtAction(nameof(GetByExternalId), new { externalOperationId = externalId }, new
                {
                    externalOperationId = externalId,
                    createdAt = op.CreatedAt,
                    status = op.Status
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear el pago");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("{externalOperationId:guid}")]
        public async Task<IActionResult> GetByExternalId(Guid externalOperationId)
        {
            var op = await _repo.GetByExternalOperationIdAsync(externalOperationId);
            if (op == null) return NotFound();
            return Ok(new { externalOperationId = op.ExternalOperationId, createdAt = op.CreatedAt, status = op.Status, updatedAt = op.UpdatedAt });
        }
    }
}
