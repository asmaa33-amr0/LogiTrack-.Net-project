using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LogiTrackWebV1._0
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LT_DBContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Apply pending EF migrations before seeding Identity data.
            await context.Database.MigrateAsync();

            // 1) Roles
            string[] roles = { "Admin", "Driver", "Customer", "Warehouse", "Station" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // 2) Admin from DemoAuth config
            var adminEmail = configuration["DemoAuth:AdminEmail"] ?? "admin@logitrack.com";
            var adminUsername = configuration["DemoAuth:AdminUsername"] ?? "admin";
            var adminPassword = configuration["DemoAuth:AdminPassword"] ?? "LogiTrack@123";

            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new IdentityUser
                {
                    UserName = adminUsername,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    SecurityStamp = Guid.NewGuid().ToString()
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (!result.Succeeded)
                    throw new InvalidOperationException(
                        "Could not create the configured admin account: " +
                        string.Join("; ", result.Errors.Select(e => e.Description)));
            }

            if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}