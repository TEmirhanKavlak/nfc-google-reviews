using System.ComponentModel.DataAnnotations;
namespace NfcDegerlendirme.Models;

public enum Role { SuperAdmin, BusinessAdmin }
public enum FeedbackStatus { New, Read, Resolved, Archived }
public enum SubStatus { Trial, Active, PastDue, Canceled }

public class User {
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(200)] public string Email { get; set; } = "";
    [MaxLength(100)] public string PasswordHash { get; set; } = "";
    [MaxLength(80)] public string Name { get; set; } = "";
    public Role Role { get; set; } = Role.BusinessAdmin;
    public Guid? BusinessId { get; set; }
    public Business? Business { get; set; }
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class Business {
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(80)] public string Slug { get; set; } = "";
    [MaxLength(100)] public string Name { get; set; } = "";
    public bool Active { get; set; } = true;
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<User> Users { get; set; } = new();
    public List<Branch> Branches { get; set; } = new();
    public List<NfcCard> Cards { get; set; } = new();
    public List<Feedback> Feedbacks { get; set; } = new();
    public BusinessSettings? Settings { get; set; }
    public Subscription? Subscription { get; set; }
}
public class Branch {
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BusinessId { get; set; }
    public Business Business { get; set; } = null!;
    [MaxLength(100)] public string Name { get; set; } = "";
    [MaxLength(300)] public string? Address { get; set; }
    public List<NfcCard> Cards { get; set; } = new();
}
public class NfcCard {
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(64)] public string Code { get; set; } = "";
    [MaxLength(60)] public string Name { get; set; } = "";
    public Guid BusinessId { get; set; }
    public Business Business { get; set; } = null!;
    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }
    public bool Active { get; set; } = true;
    public int VisitCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Feedback> Feedbacks { get; set; } = new();
}
public class Feedback {
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BusinessId { get; set; }
    public Business Business { get; set; } = null!;
    public Guid? CardId { get; set; }
    public NfcCard? Card { get; set; }
    public int Rating { get; set; }
    [MaxLength(1500)] public string? Comment { get; set; }
    [MaxLength(80)] public string? Name { get; set; }
    [MaxLength(120)] public string? Email { get; set; }
    public FeedbackStatus Status { get; set; } = FeedbackStatus.New;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<AdminNote> Notes { get; set; } = new();
}
public class BusinessSettings {
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BusinessId { get; set; }
    public Business Business { get; set; } = null!;
    [MaxLength(300)] public string? LogoUrl { get; set; }
    [MaxLength(300)] public string? CoverUrl { get; set; }
    [MaxLength(7)] public string PrimaryColor { get; set; } = "#1a73e8";
    [MaxLength(500)] public string? GoogleReviewUrl { get; set; }
    public bool CommentRequired { get; set; }
    [MaxLength(300)] public string? KvkkUrl { get; set; }
    [MaxLength(30)] public string? Phone { get; set; }
}
public class AdminNote {
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FeedbackId { get; set; }
    public Feedback Feedback { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    [MaxLength(1000)] public string Text { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class Subscription {
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BusinessId { get; set; }
    public Business Business { get; set; } = null!;
    public SubStatus Status { get; set; } = SubStatus.Trial;
    [MaxLength(40)] public string Plan { get; set; } = "basic";
    public DateTime? ExpiresAt { get; set; }
}
public class AuditLog {
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    [MaxLength(60)] public string Action { get; set; } = "";
    [MaxLength(100)] public string? Target { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
