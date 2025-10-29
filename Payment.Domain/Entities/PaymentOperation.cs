using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payment.Domain.Entities
{
    public class PaymentOperation
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ExternalOperationId { get; set; }
        public Guid CustomerId { get; set; }
        public Guid ServiceProviderId { get; set; }
        public int PaymentMethodId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = "evaluating";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
