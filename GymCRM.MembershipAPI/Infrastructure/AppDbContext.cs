using GymCRM.MembershipAPI.Models.Configurations;
using GymCRM.MembershipAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymCRM.MembershipAPI.Models
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
			
			modelBuilder.ApplyConfiguration(new AccountsConfiguration());
			modelBuilder.ApplyConfiguration(new MembersConfiguration());
		}
	}
}
