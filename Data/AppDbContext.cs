using Ecommerce.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<User> Users => Set<User>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<CartItem>()
            .HasIndex(c => new { c.UserId, c.ProductId })
            .IsUnique();

        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Electronics", Slug = "electronics" },
            new Category { Id = 2, Name = "Accessories", Slug = "accessories" }
        );

        modelBuilder.Entity<Product>().HasData(
            new Product
            {
                Id = 1,
                Name = "Mechanical Keyboard",
                Description = "Hot-swappable tactile switches with aluminum frame.",
                Price = 89.99m,
                StockQuantity = 25,
                ImageUrl = "https://placehold.co/400x300?text=Keyboard",
                CategoryId = 1
            },
            new Product
            {
                Id = 2,
                Name = "USB-C Hub",
                Description = "7-in-1 adapter with 4K HDMI and 100W power delivery.",
                Price = 34.50m,
                StockQuantity = 60,
                ImageUrl = "https://placehold.co/400x300?text=USB-C+Hub",
                CategoryId = 2
            }
        );
    }
}