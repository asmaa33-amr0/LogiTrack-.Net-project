using LogiTrackWebV1._0.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogiTrackWebV1._0.Configurations
{
    internal class ShipmentConfig : IEntityTypeConfiguration<Shipment>
    {
        public void Configure(EntityTypeBuilder<Shipment> builder)
        {
            builder.HasKey(s => s.Id);

            builder.HasIndex(s => s.TrackingNumber).IsUnique();

            builder.Property(s => s.TrackingNumber)
                   .IsRequired()
                   .HasMaxLength(50);

            builder.Property(s => s.Weight)
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();

            builder.Property(s => s.Price)
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();

            builder.Property(s => s.Status)
                   .IsRequired()
                   .HasMaxLength(50);

            // Owned Address — optional so shipments without an address can still be saved.
            builder.OwnsOne(s => s.Address, address =>
            {
                address.Property(a => a.City)
                       .HasColumnName("Shipping_City")
                       .HasMaxLength(100);

                address.Property(a => a.Street)
                       .HasColumnName("Shipping_Street")
                       .HasMaxLength(200);

                address.Property(a => a.Building)
                       .HasColumnName("Shipping_Building")
                       .HasMaxLength(50);
            });
        }
    }
}