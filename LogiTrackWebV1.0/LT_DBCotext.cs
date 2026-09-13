using LogiTrackWebV1._0.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LogiTrackWebV1._0
{
    public class LT_DBContext : IdentityDbContext<IdentityUser>
    {
        public LT_DBContext(DbContextOptions<LT_DBContext> options) : base(options)
        {
        }

        public DbSet<Shipment> Shipments { get; set; }
        public DbSet<Driver> Drivers { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<Station> Stations { get; set; }
        public DbSet<RouteCheckpoint> RouteCheckpoints { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // تطبيق كل الـ Fluent API Configurations المودودة في الـ Assembly
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(LT_DBContext).Assembly);
        }
    }
}