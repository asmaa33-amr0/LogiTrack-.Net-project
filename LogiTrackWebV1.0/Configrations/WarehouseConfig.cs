using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LogiTrackWebV1._0.Models;

namespace LogiTrackWebV1._0.Configurations
{
    internal class WarehouseConfig : IEntityTypeConfiguration<Warehouse>
    {
        public void Configure(EntityTypeBuilder<Warehouse> builder)
        {
            builder.HasKey(w => w.Id);

            builder.HasMany(w => w.Shipments)
                   .WithOne(sh => sh.Warehouse)
                   .HasForeignKey(sh => sh.WarehouseId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}