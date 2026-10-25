using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NfcDegerlendirme.Data;
using NfcDegerlendirme.Models;
using NfcDegerlendirme.Services;
namespace NfcDegerlendirme.Controllers;

[Authorize, Route("panel")]
public class PanelController : AppController {
    const int MaxLogo = 2 * 1024 * 1024;
    readonly string _logoDir;
    public PanelController(AppDbContext db, IConfiguration cfg, IWebHostEnvironment env) : base(db) { _logoDir = LogoController.Dir(cfg, env); }

    [HttpGet("")]
    public async Task<IActionResult> Index(Guid? b, string? q, int? r) {
        var id = Scope(b);
        if (id is null) return IsSuper ? Redirect("/admin") : Forbid();
        var biz = await Db.Businesses.AsNoTracking().Include(x => x.Settings).FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null);
        if (biz is null) return NotFound();
        var fq = Db.Feedbacks.AsNoTracking().Where(f => f.BusinessId == id);
        var list = fq;
        if (r is >= 1 and <= 5) list = list.Where(f => f.Rating == r);
        q = q?.Trim(); if (q is { Length: > 100 }) q = q[..100];
        if (!string.IsNullOrEmpty(q)) list = list.Where(f => f.Comment != null && EF.Functions.Like(f.Comment, "%" + q + "%"));
        var vm = new PanelVm {
            BusinessId = biz.Id, BusinessName = biz.Name, IsSuper = IsSuper, Q = q, R = r,
            Total = await fq.CountAsync(),
            Average = await fq.Select(f => (double?)f.Rating).AverageAsync() ?? 0,
            Visits = await Db.Cards.Where(c => c.BusinessId == id).SumAsync(c => (int?)c.VisitCount) ?? 0,
            Dist = await fq.GroupBy(f => f.Rating).Select(g => new { g.Key, C = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.C),
            Items = await list.OrderByDescending(f => f.CreatedAt).Take(100)
                .Select(f => new FeedbackRow(f.Rating, f.Comment, f.Name, f.Card != null ? f.Card.Name : null, f.CreatedAt)).ToListAsync(),
            LogoUrl = biz.Settings?.LogoUrl, GoogleReviewUrl = biz.Settings?.GoogleReviewUrl,
            CommentRequired = biz.Settings?.CommentRequired ?? false, PrimaryColor = Helpers.SafeColor(biz.Settings?.PrimaryColor),
        };
        return View(vm);
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(Guid? b) {
        var id = Scope(b); if (id is null) return BadRequest();
        var rows = await Db.Feedbacks.AsNoTracking().Where(f => f.BusinessId == id).OrderByDescending(f => f.CreatedAt)
            .Select(f => new { f.CreatedAt, f.Rating, f.Comment, f.Name, f.Email, Card = f.Card != null ? f.Card.Name : null, f.Status }).ToListAsync();
        var sb = new StringBuilder("Tarih,Puan,Yorum,Ad,E-posta,Kart,Durum\n");
        foreach (var x in rows)
            sb.Append(string.Join(",", new[] { x.CreatedAt.ToString("o"), x.Rating.ToString(), x.Comment, x.Name, x.Email, x.Card, x.Status.ToString() }.Select(Helpers.CsvCell))).Append('\n');
        return File(new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray(), "text/csv; charset=utf-8", "geri-bildirimler.csv");
    }

    async Task<BusinessSettings?> GetSettings(Guid id) {
        if (!await Db.Businesses.AnyAsync(x => x.Id == id && x.DeletedAt == null)) return null;
        var s = await Db.Settings.FirstOrDefaultAsync(x => x.BusinessId == id);
        if (s is null) { s = new BusinessSettings { BusinessId = id }; Db.Settings.Add(s); }
        return s;
    }

    [HttpPost("settings")]
    public async Task<IActionResult> SaveSettings(Guid? b, string? googleReviewUrl, bool commentRequired, string? primaryColor) {
        var id = Scope(b); if (id is null) return BadRequest();
        var url = string.IsNullOrWhiteSpace(googleReviewUrl) ? null : Helpers.SafeGoogleUrl(googleReviewUrl);
        if (!string.IsNullOrWhiteSpace(googleReviewUrl) && url is null) { TempData["Err"] = "Geçerli bir https Google bağlantısı girin."; return Redirect(Back(b)); }
        var s = await GetSettings(id.Value); if (s is null) return NotFound();
        s.GoogleReviewUrl = url; s.CommentRequired = commentRequired; s.PrimaryColor = Helpers.SafeColor(primaryColor);
        Audit("settings.update", id.ToString());
        await Db.SaveChangesAsync();
        TempData["Ok"] = "Ayarlar kaydedildi.";
        return Redirect(Back(b));
    }

    [HttpPost("logo")]
    public async Task<IActionResult> UploadLogo(Guid? b, IFormFile? logo) {
        var id = Scope(b); if (id is null) return BadRequest();
        if (logo is null || logo.Length == 0) { TempData["Err"] = "Dosya seçilmedi."; return Redirect(Back(b)); }
        if (logo.Length > MaxLogo) { TempData["Err"] = "Dosya en fazla 2 MB olabilir."; return Redirect(Back(b)); }
        using var ms = new MemoryStream(); await logo.CopyToAsync(ms);
        var bytes = ms.ToArray(); var ext = Helpers.DetectImage(bytes);
        if (ext is null) { TempData["Err"] = "Yalnızca PNG, JPG veya WebP yüklenebilir."; return Redirect(Back(b)); }
        var s = await GetSettings(id.Value); if (s is null) return NotFound();
        Directory.CreateDirectory(_logoDir);
        foreach (var e in LogoController.Exts) { var old = Path.Combine(_logoDir, $"{id}.{e}"); if (System.IO.File.Exists(old)) System.IO.File.Delete(old); }
        await System.IO.File.WriteAllBytesAsync(Path.Combine(_logoDir, $"{id}.{ext}"), bytes);
        s.LogoUrl = $"/logo/{id}?v={DateTime.UtcNow.Ticks}";
        Audit("logo.upload", id.ToString()); await Db.SaveChangesAsync();
        TempData["Ok"] = "Profil fotoğrafı güncellendi.";
        return Redirect(Back(b));
    }

    [HttpPost("logo/sil")]
    public async Task<IActionResult> RemoveLogo(Guid? b) {
        var id = Scope(b); if (id is null) return BadRequest();
        foreach (var e in LogoController.Exts) { var p = Path.Combine(_logoDir, $"{id}.{e}"); if (System.IO.File.Exists(p)) System.IO.File.Delete(p); }
        var s = await Db.Settings.FirstOrDefaultAsync(x => x.BusinessId == id);
        if (s != null) s.LogoUrl = null;
        Audit("logo.delete", id.ToString()); await Db.SaveChangesAsync();
        TempData["Ok"] = "Profil fotoğrafı kaldırıldı.";
        return Redirect(Back(b));
    }
    string Back(Guid? b) => IsSuper && b != null ? $"/panel?b={b}" : "/panel";
}
