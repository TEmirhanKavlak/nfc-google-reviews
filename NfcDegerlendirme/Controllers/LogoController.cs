using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace NfcDegerlendirme.Controllers;

[AllowAnonymous]
public class LogoController : Controller {
    public static readonly string[] Exts = { "png", "jpg", "webp" };
    static readonly Dictionary<string, string> Types = new() { ["png"] = "image/png", ["jpg"] = "image/jpeg", ["webp"] = "image/webp" };
    public static string Dir(IConfiguration cfg, IWebHostEnvironment env) {
        var root = cfg["UploadDir"]; if (string.IsNullOrWhiteSpace(root)) root = Path.Combine(env.ContentRootPath, "uploads");
        return Path.Combine(root, "logos");
    }
    readonly string _dir;
    public LogoController(IConfiguration cfg, IWebHostEnvironment env) { _dir = Dir(cfg, env); }

    [HttpGet("/logo/{id:guid}")]
    public IActionResult Get(Guid id) {
        foreach (var e in Exts) {
            var p = Path.Combine(_dir, $"{id}.{e}");
            if (System.IO.File.Exists(p)) {
                Response.Headers.CacheControl = "public, max-age=86400";
                return PhysicalFile(p, Types[e]);
            }
        }
        return NotFound();
    }
}
