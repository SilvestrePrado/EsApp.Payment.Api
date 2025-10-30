using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payment.Domain.Dtos
{
    public record RiskEvaluationRequestDto(Guid ExternalOperationId, Guid CustomerId, decimal Amount);
    public record RiskEvaluationResponseDto(Guid ExternalOperationId, string Status);
}
