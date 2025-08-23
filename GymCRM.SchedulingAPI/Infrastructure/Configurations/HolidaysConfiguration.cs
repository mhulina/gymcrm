using GymCRM.SchedulingAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymCRM.SchedulingAPI.Infrastructure.Configurations;

public class HolidaysConfiguration : IEntityTypeConfiguration<Holiday>
{
    public void Configure(EntityTypeBuilder<Holiday> modelBuilder)
    {
        modelBuilder.ToTable("Holidays");
        
        modelBuilder.HasKey(x => x.Id);
        
        modelBuilder.Property(x => x.Date).IsRequired();
        modelBuilder.Property(x => x.Created).IsRequired();
        modelBuilder.Property(x => x.Type).IsRequired();
        modelBuilder.Property(x => x.LocalName).IsRequired();
        modelBuilder.Property(x => x.EnglishName).IsRequired();
        modelBuilder.Property(x => x.CountryCode).IsRequired();
        modelBuilder.Property(x => x.RegionCode).IsRequired();
        modelBuilder.Property(x => x.Year).IsRequired();
    }
}