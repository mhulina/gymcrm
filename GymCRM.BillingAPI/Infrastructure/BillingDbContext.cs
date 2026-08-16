using GymCRM.BillingAPI.Infrastructure.Configurations;
using GymCRM.BillingAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymCRM.BillingAPI.Infrastructure
{
    public class BillingDbContext : DbContext
    {
        public virtual DbSet<Subscription> Subscriptions { get; set; }
        public virtual DbSet<Payment> Payments { get; set; }

        public BillingDbContext(DbContextOptions<BillingDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasDefaultSchema("billing_db");
            modelBuilder.ApplyConfiguration(new SubscriptionsConfiguration());
            modelBuilder.ApplyConfiguration(new PaymentsConfiguration());
        }
    }
}
