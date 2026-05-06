using System.Security.Claims;

namespace TaskFlow.Api.Auth;

public static class CurrentUser
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var raw = principal.FindFirstValue("sub")
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id)
            ? id
            : throw new UnauthorizedAccessException("User id claim missing.");
    }
}
