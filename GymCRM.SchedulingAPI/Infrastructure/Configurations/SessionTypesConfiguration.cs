using GymCRM.SchedulingAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymCRM.SchedulingAPI.Infrastructure.Configurations;

public class SessionTypesConfiguration : IEntityTypeConfiguration<SessionType>
{
    public void Configure(EntityTypeBuilder<SessionType> modelBuilder)
    {
        modelBuilder.ToTable("SessionTypes", "scheduling_db");
        
        modelBuilder.HasKey(x => x.Id);
        
        modelBuilder.Property(x => x.Name).HasMaxLength(160).IsRequired();
        modelBuilder.Property(x => x.DurationMinutes).IsRequired();
        modelBuilder.Property(x => x.Description).HasMaxLength(2000);
    }
}