using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Liczydelko.Kontrolery;

[AllowAnonymous, IgnoreAntiforgeryToken, ResponseCache(NoStore = true)]
public class BladController : Controller
{
    public IActionResult Index(int? kod)
    {
        ViewBag.Kod = kod ?? 500;
        return View();
    }
}
