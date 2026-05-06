using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Users;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(RegisterUserHandler register, LoginUserHandler login) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand cmd, CancellationToken ct)
        => (await register.HandleAsync(cmd, ct)).ToActionResult();

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginUserCommand cmd, CancellationToken ct)
        => (await login.HandleAsync(cmd, ct)).ToActionResult();
}
