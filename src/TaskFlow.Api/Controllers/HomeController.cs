using Microsoft.AspNetCore.Mvc;

namespace TaskFlow.Api.Controllers;

public sealed class HomeController : Controller
{
    public IActionResult Index() => View();
}
