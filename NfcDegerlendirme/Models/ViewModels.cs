using System.ComponentModel.DataAnnotations;
namespace NfcDegerlendirme.Models;

public record ReviewVm(string Code, string BusinessName, string? LogoUrl, string? CoverUrl, string PrimaryColor, bool CommentRequired, string? KvkkUrl);
public class FeedbackDto {
    public string? Code { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
}
public record FeedbackRow(int Rating, string? Comment, string? Name, string? CardName, DateTime CreatedAt);
public class PanelVm {
    public Guid BusinessId { get; set; }
    public string BusinessName { get; set; } = "";
    public bool IsSuper { get; set; }
    public int Visits { get; set; }
    public int Total { get; set; }
    public double Average { get; set; }
    public Dictionary<int, int> Dist { get; set; } = new();
    public List<FeedbackRow> Items { get; set; } = new();
    public string? Q { get; set; }
    public int? R { get; set; }
    public string? LogoUrl { get; set; }
    public string? GoogleReviewUrl { get; set; }
    public bool CommentRequired { get; set; }
    public string PrimaryColor { get; set; } = "#1a73e8";
}
public record AdminRow(Guid Id, string Name, string? LogoUrl, bool Active, List<(string Name, string Code)> Cards, int Feedbacks, List<string> Admins);
public class AdminVm { public List<AdminRow> Rows { get; set; } = new(); public string AppUrl { get; set; } = ""; }
public class CreateBusinessForm {
    [Required, StringLength(100, MinimumLength = 2)] public string Name { get; set; } = "";
    [Required, StringLength(80, MinimumLength = 2)] public string AdminName { get; set; } = "";
    [Required, EmailAddress, StringLength(120)] public string AdminEmail { get; set; } = "";
    [Required, StringLength(128, MinimumLength = 8)] public string AdminPassword { get; set; } = "";
    [StringLength(60)] public string? CardName { get; set; }
}
