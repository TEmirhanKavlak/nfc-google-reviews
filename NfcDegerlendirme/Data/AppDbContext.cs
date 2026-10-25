using Microsoft.EntityFrameworkCore;
using NfcDegerlendirme.Models;
namespace NfcDegerlendirme.Data;

public class AppDbContext : DbContext {
    public AppDbContext(DbContextOptions<AppDbContext> o) : base(o) { }
    public DbSet<User> Users => Set<User>();
    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<NfcCard> Cards => Set<NfcCard>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();
    public DbSet<BusinessSettings> Settings => Set<BusinessSettings>();
    public DbSet<AdminNote> Notes => Set<AdminNote>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder b) {
        b.Entity<User>().HasIndex(x => x.Email).IsUnique();
        b.Entity<User>().Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
        b.Entity<User>().HasOne(x => x.Business).WithMany(x => x.Users).HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Business>().HasIndex(x => x.Slug).IsUnique();
        b.Entity<Branch>().HasOne(x => x.Business).WithMany(x => x.Branches).HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<NfcCard>().HasIndex(x => x.Code).IsUnique();
        b.Entity<NfcCard>().HasOne(x => x.Business).WithMany(x => x.Cards).HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<NfcCard>().HasOne(x => x.Branch).WithMany(x => x.Cards).HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.SetNull);
        b.Entity<Feedback>().HasIndex(x => new { x.BusinessId, x.CreatedAt });
        b.Entity<Feedback>().HasIndex(x => new { x.BusinessId, x.Rating });
        b.Entity<Feedback>().Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Entity<Feedback>().HasOne(x => x.Business).WithMany(x => x.Feedbacks).HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Feedback>().HasOne(x => x.Card).WithMany(x => x.Feedbacks).HasForeignKey(x => x.CardId).OnDelete(DeleteBehavior.SetNull);
        b.Entity<BusinessSettings>().HasIndex(x => x.BusinessId).IsUnique();
        b.Entity<BusinessSettings>().HasOne(x => x.Business).WithOne(x => x.Settings).HasForeignKey<BusinessSettings>(x => x.BusinessId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Subscription>().HasIndex(x => x.BusinessId).IsUnique();
        b.Entity<Subscription>().Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Entity<Subscription>().HasOne(x => x.Business).WithOne(x => x.Subscription).HasForeignKey<Subscription>(x => x.BusinessId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<AdminNote>().HasOne(x => x.Feedback).WithMany(x => x.Notes).HasForeignKey(x => x.FeedbackId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<AdminNote>().HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<AuditLog>().HasIndex(x => x.CreatedAt);
    }
}
