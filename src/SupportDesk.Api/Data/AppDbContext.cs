using Microsoft.EntityFrameworkCore;
using SupportDesk.Api.Domain;

namespace SupportDesk.Api.Data;

// Доступ к хранилищу SQLite. К нему обращается только веб-API (TB-3).
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Session> Sessions => Set<Session>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>().HasIndex(a => a.Login).IsUnique();
        modelBuilder.Entity<Account>().Property(a => a.Role).HasConversion<string>();

        modelBuilder.Entity<Session>()
            .HasOne(s => s.Account)
            .WithMany()
            .HasForeignKey(s => s.AccountId);
    }
}
