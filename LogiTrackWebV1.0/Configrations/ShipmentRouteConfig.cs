using LogiTrackWebV1._0.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder;
namespace LogiTrackWebV1._0.Configrations
{
    internal class ShipmentRouteConfig:IEntityTypeConfiguration<ShipmentRoute_mtm>
    {

        public void Configure(EntityTypeBuilder<ShipmentRoute_mtm> builder)
        {
            builder.HasKey(sr => new { sr.ShipmentId, sr.StationId });

            builder.HasOne(sr => sr.Shipment)
                .WithMany(sh => sh.ShipmentRoutes)
                .HasForeignKey(sr => sr.ShipmentId);


            builder.HasOne(sr => sr.Station)
               .WithMany(st => st.ShipmentRoutes)
               .HasForeignKey(sr => sr.StationId);

        }
    }
}
