using Microsoft.AspNetCore.Mvc;

namespace TheCrewCommunity.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}