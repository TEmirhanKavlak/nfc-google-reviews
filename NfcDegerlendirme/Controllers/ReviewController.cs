using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NfcDegerlendirme.Data;
using NfcDegerlendirme.Models;
using NfcDegerlendirme.Services;
namespace NfcDegerlendirme.Controllers;

[AllowAnonymous]
public class ReviewController : Controller {
    static readonly Regex EmailRx = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
    readonly AppDbContext _db; readonly IMemoryCache _cache;
    public ReviewController(AppDbContext db, IMemoryCache cache) { _db = db; _cache = cache; }

    [HttpGet("/r/{code}")]
    public async Task<IActionResult> Index(string code) {
        if (code.Length is < 8 or > 64) return NotFound();
        var card = await _db.Cards.AsNoTracking().Include(c => c.Business).ThenInclude(b => b.Settings).FirstOrDefaultAsync(c => c.Code == code);
        if (card is null || !card.Active || !card.Business.Active || card.Business.DeletedAt != null) return NotFound();
        await _db.Cards.Where(c => c.Id == card.Id).ExecuteUpdateAsync(s => s.SetProperty(c => c.VisitCount, c => c.VisitCount + 1));
        var st = card.Business.Settings;
        return View(new ReviewVm(card.Code, card.Business.Name, st?.LogoUrl, st?.CoverUrl, Helpers.SafeColor(st?.PrimaryColor), st?.CommentRequired ?? false, st?.KvkkUrl));
    }

    [HttpPost("/api/feedback"), EnableRateLimiting("feedback")]
    public async Task<IActionResult> Submit([FromBody] FeedbackDto? d) {
        if (d is null || !string.IsNullOrEmpty(d.Website)) return BadRequest(new { error = "Geçersiz form verisi." });
        var comment = Helpers.CleanText(d.Comment); var name = Helpers.CleanText(d.Name); var email = d.Email?.Trim();
        if (string.IsNullOrEmpty(d.Code) || d.Code.Length is < 8 or > 64 || d.Rating is < 1 or > 5
            || (comment?.Length ?? 0) > 1500 || (name?.Length ?? 0) > 80
            || (!string.IsNullOrEmpty(email) && (email.Length > 120 || !EmailRx.IsMatch(email))))
            return BadRequest(new { error = "Geçersiz form verisi." });
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "?";
        var key = "dup:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{ip}|{d.Code}|{d.Rating}|{comment}")));
        if (_cache.TryGetValue(key, out _)) return StatusCode(429, new { error = "Bu değerlendirme zaten gönderildi." });
        _cache.Set(key, true, TimeSpan.FromSeconds(30));
        var card = await _db.Cards.Include(c => c.Business).ThenInclude(b => b.Settings).FirstOrDefaultAsync(c => c.Code == d.Code);
        if (card is null || !card.Active || !card.Business.Active || card.Business.DeletedAt != null) return NotFound(new { error = "Kart geçersiz." });
        var st = card.Business.Settings;
        if (st?.CommentRequired == true && comment is null) return BadRequest(new { error = "Yorum zorunlu." });
        // İşletme ve kart bilgisi istemciden değil, karttan alınır.
        _db.Feedbacks.Add(new Feedback { BusinessId = card.BusinessId, CardId = card.Id, Rating = d.Rating, Comment = comment,
            Name = name, Email = string.IsNullOrEmpty(email) ? null : email });
        await _db.SaveChangesAsync();
        var google = Helpers.GoogleRedirect(d.Rating, st?.GoogleReviewUrl);
        return Json(new { ok = true, google, googleMissing = d.Rating >= 4 && google is null });
    }
}
