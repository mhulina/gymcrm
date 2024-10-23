using GymCRM.MembershipAPI.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymCRM.MembershipAPI.Infrastructure.Configurations
{
	public class UsersConfiguration : IEntityTypeConfiguration<User>
	{
		public void Configure(EntityTypeBuilder<User> modelBuilder)
		{
			modelBuilder.ToTable("GymUsers");

			modelBuilder.HasKey(x => x.Id);
			modelBuilder.HasIndex(x => x.Guid, "IX_Guid").IsUnique();

			modelBuilder.Property(x => x.Id).ValueGeneratedOnAdd();
			modelBuilder.Property(x => x.Guid).IsRequired();
			modelBuilder.Property(x => x.UserType).IsRequired();
			modelBuilder.Property(x => x.FirstName).IsRequired();
			modelBuilder.Property(x => x.LastName).IsRequired();
			modelBuilder.Property(x => x.Email).IsRequired();
			modelBuilder.Property(x => x.PhoneNumber).IsRequired();
			modelBuilder.Property(x => x.DateJoined).IsRequired();

			modelBuilder.HasData(
				new User
				{
					Id = 1,
					Guid = Guid.NewGuid(),
					DateJoined = DateTime.Today.Date.ToUniversalTime(),
					FirstName = "Admin",
					LastName = "Adminski",
					Email = "test@test.com",
					UserType = 1,
					PhoneNumber = "123456789"
				});
		}
	}
}
