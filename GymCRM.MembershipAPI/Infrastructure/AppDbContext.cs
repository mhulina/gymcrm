using GymCRM.MembershipAPI.Infrastructure.Configurations;
using GymCRM.MembershipAPI.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymCRM.MembershipAPI.Infrastructure
{
    public class AppDbContext : DbContext
	{
		public DbSet<Member> Members { get; set; }
		
		public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) 
		{
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			modelBuilder.ApplyConfiguration(new MembersConfiguration());
		}
	}
}
