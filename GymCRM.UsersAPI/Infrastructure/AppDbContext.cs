using GymCRM.UsersAPI.Infrastructure.Configurations;
using GymCRM.UsersAPI.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymCRM.UsersAPI.Infrastructure
{
    public class AppDbContext : DbContext
	{
		public DbSet<User> GymUsers { get; set; }
		
		public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) 
		{
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			modelBuilder.ApplyConfiguration(new UsersConfiguration());
		}
	}
}
