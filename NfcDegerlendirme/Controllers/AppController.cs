using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using NfcDegerlendirme.Data;
using NfcDegerlendirme.Models;
namespace NfcDegerlendirme.Controllers;

public abstract class AppController : Controller {
    protected readonly AppDbContext Db;
    protected AppController(AppDbContext db) { Db = db; }
    protected Guid? CurrentUserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var g) ? g : null;
    protected bool IsSuper => User.IsInRole("SuperAdmin");
    // Firma izolasyonu: firma yöneticisi için kimlik YALNIZCA oturumdan gelir; parametre yalnızca süper admin içindir.
    protected Guid? Scope(Guid? requested) => IsSuper ? requested : Guid.TryParse(User.FindFirstValue("biz"), out var g) ? g : null;
    protected void Audit(string action, string? target) => Db.AuditLogs.Add(new AuditLog { UserId = CurrentUserId, Action = action, Target = target });
}
