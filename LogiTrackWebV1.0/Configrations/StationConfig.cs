using LogiTrackWebV1._0.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogiTrackWebV1._0.Configrations
{
    internal class StationConfig : IEntityTypeConfiguration<Station>

    {
        public void Configure(EntityTypeBuilder<Station> builder)
        {
            builder.HasKey(s => s.Id);
            builder.Property(s => s.StationName)
                 .IsRequired()
                 .HasMaxLength(150);
        }
    }
}
