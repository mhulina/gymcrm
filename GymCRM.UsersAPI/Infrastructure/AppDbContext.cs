using GymCRM.UsersAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace GymCRM.UsersAPI.Infrastructure
{
	public class AppDbContext : DbContext
	{
		public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) 
		{
		}

		public DbSet<User> GymUsers { get; set; }
	}
}
