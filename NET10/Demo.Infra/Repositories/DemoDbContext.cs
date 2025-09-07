using Demo.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Demo.Infra.Repositories;

public class DemoDbContext : DbContext
{
    public DbSet<UserEntity> Users { get; set; }
    public DbSet<Company> Companies { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    
    public DemoDbContext(DbContextOptions<DemoDbContext> options)
        : base(options)
    {
    }
}
