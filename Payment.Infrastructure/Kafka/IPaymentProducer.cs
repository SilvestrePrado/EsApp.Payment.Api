using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Payment.Domain.Dtos;
using System.Threading;


namespace Payment.Infrastructure.Kafka
{
    public interface IPaymentProducer
    {
        Task ProduceRiskRequestAsync(RiskEvaluationRequestDto dto, CancellationToken cancellationToken = default);
    }
}
