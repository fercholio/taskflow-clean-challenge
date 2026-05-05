using FluentValidation;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Common;
using TaskFlow.Domain.Common;
using TaskFlow.Domain.Users;

namespace TaskFlow.Application.Users;

public sealed class LoginUserValidator : AbstractValidator<LoginUserCommand>
{
    public LoginUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class LoginUserHandler(
    IUserRepository users,
    IPasswordHasher hasher,
    IJwtTokenService jwt,
    LoginUserValidator validator)
{
    public async Task<Result<AuthResponse>> HandleAsync(LoginUserCommand cmd, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(cmd, ct);
        if (!validation.IsValid)
        {
            return Error.Validation(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));
        }

        Email email;
        try
        {
            email = Email.Create(cmd.Email);
        }
        catch (DomainException)
        {
            return Error.Unauthorized("Invalid credentials.");
        }

        var user = await users.GetByEmailAsync(email, ct);
        if (user is null || !hasher.Verify(cmd.Password, user.PasswordHash))
        {
            return Error.Unauthorized("Invalid credentials.");
        }

        var token = jwt.CreateToken(user);
        return new AuthResponse(token.AccessToken, token.ExpiresAtUtc, user.Id, user.Email.Value);
    }
}
