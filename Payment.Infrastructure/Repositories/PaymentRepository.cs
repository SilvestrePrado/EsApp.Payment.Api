using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Payment.Domain.Entities;
using Payment.Infrastructure.Persistence;

namespace Payment.Infrastructure.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly PaymentDbContext _db;
        public PaymentRepository(PaymentDbContext db) => _db = db;

        public async Task AddAsync(PaymentOperation op) => await _db.Payments.AddAsync(op);

        public void Update(PaymentOperation op) => _db.Payments.Update(op);

        public async Task<PaymentOperation?> GetByExternalOperationIdAsync(Guid externalId) =>
            await _db.Payments.FirstOrDefaultAsync(p => p.ExternalOperationId == externalId);

        public async Task<int> SaveChangesAsync() => await _db.SaveChangesAsync();
    }
}
