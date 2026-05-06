using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/v1/ping")]
public sealed class PingController : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Anonymous() => Ok(new { status = "ok", authorized = false });

    [HttpGet("secure")]
    [Authorize]
    public IActionResult Secure() => Ok(new { status = "ok", authorized = true, user = User.Identity?.Name });
}
