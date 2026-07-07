using ChatApp.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Data;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Message> Messages => Set<Message>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Message>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Content).IsRequired();
            entity.Property(m => m.SenderEmail).IsRequired();
            entity.HasOne(m => m.Sender)
                  .WithMany()
                  .HasForeignKey(m => m.SenderId);
        });

        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(rt => rt.Id);
            entity.HasIndex(rt => rt.Token).IsUnique();
            entity.Property(rt => rt.Token).IsRequired();
            entity.HasOne(rt => rt.User)
                  .WithMany()
                  .HasForeignKey(rt => rt.UserId);
        });

        builder.Entity<PushSubscription>(entity =>
        {
            entity.HasKey(ps => ps.Id);
            entity.Property(ps => ps.Endpoint).IsRequired();
            entity.Property(ps => ps.P256DH).IsRequired();
            entity.Property(ps => ps.Auth).IsRequired();
            entity.HasOne(ps => ps.User)
                  .WithMany()
                  .HasForeignKey(ps => ps.UserId);
            entity.HasIndex(ps => new { ps.UserId, ps.Endpoint }).IsUnique();
        });
    }
}
