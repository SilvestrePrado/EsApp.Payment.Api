using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Payment.Domain.Entities;

namespace Payment.Infrastructure.Persistence
{
    public class PaymentDbContext : DbContext
    {
        public PaymentDbContext(DbContextOptions<PaymentDbContext> options)
            : base(options)
        {
        }

        public DbSet<PaymentOperation> Payments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PaymentOperation>().HasKey(p => p.Id);
            modelBuilder.Entity<PaymentOperation>().HasIndex(p => p.ExternalOperationId).IsUnique();
            base.OnModelCreating(modelBuilder);
        }
    }
}
