using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NfcDegerlendirme.Data;
using NfcDegerlendirme.Models;
using NfcDegerlendirme.Services;
namespace NfcDegerlendirme.Controllers;

[Authorize(Roles = "SuperAdmin"), Route("admin")]
public class AdminController : AppController {
    readonly IConfiguration _cfg;
    public AdminController(AppDbContext db, IConfiguration cfg) : base(db) { _cfg = cfg; }

    [HttpGet("")]
    public async Task<IActionResult> Index() {
        var raw = await Db.Businesses.AsNoTracking().Where(b => b.DeletedAt == null).OrderByDescending(b => b.CreatedAt)
            .Select(b => new { b.Id, b.Name, Logo = b.Settings != null ? b.Settings.LogoUrl : null, b.Active,
                Cards = b.Cards.Select(c => new { c.Name, c.Code }).ToList(), Fb = b.Feedbacks.Count, Admins = b.Users.Select(u => u.Email).ToList() }).ToListAsync();
        return View(new AdminVm {
            AppUrl = (_cfg["AppUrl"] ?? "").TrimEnd('/'),
            Rows = raw.Select(x => new AdminRow(x.Id, x.Name, x.Logo, x.Active, x.Cards.Select(c => (c.Name, c.Code)).ToList(), x.Fb, x.Admins)).ToList() });
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create(CreateBusinessForm f) {
        if (!ModelState.IsValid) { TempData["Err"] = "Alanları kontrol edin (şifre en az 8 karakter olmalı)."; return Redirect("/admin"); }
        var name = Helpers.CleanText(f.Name); var adminName = Helpers.CleanText(f.AdminName);
        if (name is null || adminName is null) { TempData["Err"] = "Geçersiz ad."; return Redirect("/admin"); }
        var email = f.AdminEmail.Trim().ToLowerInvariant();
        if (await Db.Users.AnyAsync(u => u.Email == email)) { TempData["Err"] = "Bu e-posta zaten kayıtlı."; return Redirect("/admin"); }
        var slug = Helpers.Slugify(name);
        if (await Db.Businesses.AnyAsync(b => b.Slug == slug)) slug += "-" + Guid.NewGuid().ToString("N")[..4];
        var card = new NfcCard { Code = Helpers.NewCardCode(), Name = Helpers.CleanText(f.CardName) ?? "Ana kart" };
        var biz = new Business { Slug = slug, Name = name, Settings = new BusinessSettings(), Subscription = new Subscription(), Cards = { card },
            Users = { new User { Email = email, Name = adminName, Role = Role.BusinessAdmin, PasswordHash = BCrypt.Net.BCrypt.HashPassword(f.AdminPassword, 12) } } };
        Db.Businesses.Add(biz); Audit("business.create", biz.Id.ToString());
        await Db.SaveChangesAsync();
        TempData["Ok"] = $"Firma eklendi. NFC adresi: {(_cfg["AppUrl"] ?? "").TrimEnd('/')}/r/{card.Code}";
        return Redirect("/admin");
    }

    [HttpPost("toggle")]
    public async Task<IActionResult> Toggle(Guid id, bool active) {
        var n = await Db.Businesses.Where(b => b.Id == id && b.DeletedAt == null).ExecuteUpdateAsync(s => s.SetProperty(b => b.Active, active));
        if (n == 0) return NotFound();
        Audit(active ? "business.activate" : "business.deactivate", id.ToString()); await Db.SaveChangesAsync();
        return Redirect("/admin");
    }

    // Soft delete: veriler silinmez, firma arşivlenir; giriş ve kartlar kapanır.
    [HttpPost("delete")]
    public async Task<IActionResult> Delete(Guid id) {
        var now = DateTime.UtcNow;
        var n = await Db.Businesses.Where(b => b.Id == id && b.DeletedAt == null).ExecuteUpdateAsync(s => s.SetProperty(b => b.DeletedAt, now).SetProperty(b => b.Active, false));
        if (n == 0) return NotFound();
        Audit("business.archive", id.ToString()); await Db.SaveChangesAsync();
        return Redirect("/admin");
    }
}
