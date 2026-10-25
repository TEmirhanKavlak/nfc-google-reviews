using Microsoft.AspNetCore.Mvc;
namespace NfcDegerlendirme.Controllers;
public class HomeController : Controller {
    [HttpGet("/")] public IActionResult Index() => Redirect("/giris");
    [HttpGet("/hata")] public IActionResult Hata() => Content("Bir hata oluştu. Lütfen daha sonra tekrar deneyin.");
}
