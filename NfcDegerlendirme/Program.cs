using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NfcDegerlendirme.Data;
using NfcDegerlendirme.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddControllersWithViews(o => o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddMemoryCache();
builder.Services.AddAuthorization();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o => {
    o.LoginPath = "/giris"; o.AccessDeniedPath = "/giris";
    o.Cookie.Name = "nfc.sess"; o.Cookie.HttpOnly = true; o.Cookie.SameSite = SameSiteMode.Strict;
    o.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    o.ExpireTimeSpan = TimeSpan.FromHours(8); o.SlidingExpiration = false;
    // Pasifleştirilen kullanıcı/firma açık oturumla da devam edemez.
    o.Events.OnValidatePrincipal = async ctx => {
        var db = ctx.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
        var ok = false;
        if (Guid.TryParse(ctx.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var uid)) {
            var u = await db.Users.AsNoTracking().Include(x => x.Business).FirstOrDefaultAsync(x => x.Id == uid);
            ok = u is { Active: true } && (u.Business == null || (u.Business.Active && u.Business.DeletedAt == null));
        }
        if (!ok) { ctx.RejectPrincipal(); await ctx.HttpContext.SignOutAsync(); }
    };
});
string Ip(HttpContext c) => c.Connection.RemoteIpAddress?.ToString() ?? "?";
builder.Services.AddRateLimiter(o => {
    o.RejectionStatusCode = 429;
    o.AddPolicy("feedback", c => RateLimitPartition.GetFixedWindowLimiter(Ip(c), _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(10) }));
    o.AddPolicy("login", c => RateLimitPartition.GetFixedWindowLimiter(Ip(c), _ => new FixedWindowRateLimiterOptions { PermitLimit = 8, Window = TimeSpan.FromMinutes(15) }));
});
// X-Forwarded-For yalnızca güvenilir proxy'den (varsayılan: yerel makine) kabul edilir.
builder.Services.Configure<ForwardedHeadersOptions>(o => o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);

var app = builder.Build();
app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/hata"); app.UseHsts(); }
app.Use(async (c, next) => {
    var h = c.Response.Headers;
    h["X-Content-Type-Options"] = "nosniff"; h["X-Frame-Options"] = "DENY"; h["Referrer-Policy"] = "same-origin";
    h["Content-Security-Policy"] = "default-src 'self'; img-src 'self' data:; style-src 'self' 'unsafe-inline'; script-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
    await next();
});
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// İlk süper admin: yalnızca SeedAdmin:Email ve SeedAdmin:Password tanımlıysa ve hiç süper admin yoksa.
using (var scope = app.Services.CreateScope()) {
    try {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var email = app.Configuration["SeedAdmin:Email"]?.Trim().ToLowerInvariant();
        var pw = app.Configuration["SeedAdmin:Password"];
        if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(pw) && !await db.Users.AnyAsync(u => u.Role == Role.SuperAdmin)) {
            db.Users.Add(new User { Email = email, Name = "Süper Admin", Role = Role.SuperAdmin, PasswordHash = BCrypt.Net.BCrypt.HashPassword(pw, 12) });
            await db.SaveChangesAsync();
            app.Logger.LogInformation("İlk süper admin oluşturuldu.");
        }
    } catch (Exception e) { app.Logger.LogWarning("Seed atlandı (veritabanı hazır mı? 'dotnet ef database update' çalıştırın): {Msg}", e.Message); }
}
app.Run();
