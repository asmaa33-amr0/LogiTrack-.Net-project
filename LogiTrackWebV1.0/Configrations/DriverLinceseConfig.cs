using LogiTrackWebV1._0.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogiTrackWebV1._0.Configurations
{
    internal class DriverLicenseConfig : IEntityTypeConfiguration<DriverLicense>
    {
        public void Configure(EntityTypeBuilder<DriverLicense> builder)
        {
            builder.HasKey(l => l.DriverId);

            builder.Property(l => l.LicenseNumber)
                   .IsRequired()
                   .HasMaxLength(50);

            builder.HasIndex(l => l.LicenseNumber).IsUnique();

            builder.Property(l => l.ExpiryDate).IsRequired();
        }
    }
}