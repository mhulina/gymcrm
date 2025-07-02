using GymCRM.MembershipAPI.Infrastructure.Configurations;
using GymCRM.MembershipAPI.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymCRM.MembershipAPI.Infrastructure
{
    public class AppDbContext : DbContext
	{
		public virtual DbSet<Member> Members { get; set; }
		public virtual DbSet<Account> Accounts { get; set; }
		
		public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) 
		{
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			var initialAccountEmail = "test@test.com";
			var initialAccountDateTime = DateTime.UtcNow;
			var initialAccountGuid = Guid.NewGuid();
			
			modelBuilder.ApplyConfiguration(
				new AccountsConfiguration(
					initialAccountDateTime, 
					initialAccountEmail,
					initialAccountGuid));
			modelBuilder.ApplyConfiguration(new MembersConfiguration(initialAccountEmail, initialAccountGuid));
		}
	}
}
