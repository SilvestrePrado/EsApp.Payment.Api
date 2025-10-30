using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Payment.Domain.Entities;
using System.Threading.Tasks;

namespace Payment.Infrastructure.Repositories
{
    public interface IPaymentRepository
    {
        Task AddAsync(PaymentOperation op);
        Task<PaymentOperation?> GetByExternalOperationIdAsync(Guid externalId);
        void Update(PaymentOperation op);
        Task<int> SaveChangesAsync();
    }
}
