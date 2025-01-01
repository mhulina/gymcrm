using System.Security.Cryptography;
using System.Text;
using GymCRM.MembershipAPI.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymCRM.MembershipAPI.Infrastructure.Configurations
{
	public class MembersConfiguration : IEntityTypeConfiguration<Member>
	{
		public void Configure(EntityTypeBuilder<Member> modelBuilder)
		{
			modelBuilder.ToTable("Members");

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
			modelBuilder.Property(x => x.Gender).IsRequired();
			modelBuilder.Property(x => x.HashedPassword).IsRequired();

			modelBuilder.HasData(
				new Member
				{
					Id = 1,
					Guid = Guid.NewGuid(),
					DateJoined = DateTime.Today.Date.ToUniversalTime(),
					FirstName = "Admin",
					LastName = "Adminski",
					Email = "test@test.com",
					UserType = 1,
					PhoneNumber = "123456789",
					HashedPassword = new ASCIIEncoding().GetString(
						new MD5CryptoServiceProvider().ComputeHash(
							Encoding.ASCII.GetBytes("admin"))),
					Gender = 1
				});
		}
	}
}
