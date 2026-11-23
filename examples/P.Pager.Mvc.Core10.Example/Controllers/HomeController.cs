using Microsoft.AspNetCore.Mvc;
using P.Pager.Mvc.Core10.Example.Models;
using System.Diagnostics;

namespace P.Pager.Mvc.Core10.Example.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
