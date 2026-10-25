using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using NfcDegerlendirme.Data;
namespace NfcDegerlendirme.Controllers;

public class AccountController : AppController {
    public AccountController(AppDbContext db) : base(db) { }
    [HttpGet("/giris")] public IActionResult Login() => View();

    [HttpPost("/giris"), EnableRateLimiting("login")]
    public async Task<IActionResult> Login(string? email, string? password) {
        var e = (email ?? "").Trim().ToLowerInvariant();
        var u = e.Length > 0 ? await Db.Users.Include(x => x.Business).FirstOrDefaultAsync(x => x.Email == e) : null;
        var ok = u is { Active: true } && (u.Business == null || (u.Business.Active && u.Business.DeletedAt == null))
                 && !string.IsNullOrEmpty(password) && BCrypt.Net.BCrypt.Verify(password, u.PasswordHash);
        if (!ok || u is null) { ViewBag.Error = "E-posta veya şifre hatalı."; return View(); }
        var claims = new List<Claim> {
            new(ClaimTypes.NameIdentifier, u.Id.ToString()), new(ClaimTypes.Name, u.Name), new(ClaimTypes.Role, u.Role.ToString()) };
        if (u.BusinessId is { } b) claims.Add(new Claim("biz", b.ToString()));
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
        Db.AuditLogs.Add(new Models.AuditLog { UserId = u.Id, Action = "login" });
        await Db.SaveChangesAsync();
        return Redirect("/panel");
    }
    [HttpPost("/cikis")] public async Task<IActionResult> Logout() {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/giris");
    }
}
