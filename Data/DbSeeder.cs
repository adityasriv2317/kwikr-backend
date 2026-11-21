using Ecommerce.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAdminAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();

        var adminEmail = "admin@kwikr.local";
        var seedAdmin = await db.Users.FirstOrDefaultAsync(u =>
            u.Email.ToLower() == adminEmail);

        if (seedAdmin == null)
        {
            var hasAdmin = await db.Users.AnyAsync(u => u.Role == "Admin");
            if (!hasAdmin)
            {
                db.Users.Add(new User
                {
                    FullName = "Kwikr Admin",
                    Email = adminEmail,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    Role = "Admin"
                });
                await db.SaveChangesAsync();
            }
        }
    }
}
