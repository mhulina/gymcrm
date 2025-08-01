using GymCRM.IdentityAPI.Infrastructure.Configurations;
using GymCRM.IdentityAPI.Models.Configurations;
using GymCRM.IdentityAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymCRM.IdentityAPI.Infrastructure
{
    public class IdentityDbContext : DbContext
	{
		public virtual DbSet<Member> Members { get; set; }
		public virtual DbSet<Account> Accounts { get; set; }
		
		public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options) 
		{
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);
			
			modelBuilder.HasDefaultSchema("identity_db");
			modelBuilder.ApplyConfiguration(new AccountsConfiguration());
			modelBuilder.ApplyConfiguration(new MembersConfiguration());
		}
	}
}
