using FluentValidation;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Common;
using TaskFlow.Domain.Common;
using TaskFlow.Domain.Users;

namespace TaskFlow.Application.Users;

public sealed class RegisterUserValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
    }
}

public sealed class RegisterUserHandler(
    IUserRepository users,
    IPasswordHasher hasher,
    IJwtTokenService jwt,
    IClock clock,
    RegisterUserValidator validator)
{
    public async Task<Result<AuthResponse>> HandleAsync(RegisterUserCommand cmd, CancellationToken ct)
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
        catch (DomainException ex)
        {
            return Error.Validation(ex.Message);
        }

        var existing = await users.GetByEmailAsync(email, ct);
        if (existing is not null)
        {
            return Error.Conflict("Email already registered.");
        }

        var hash = hasher.Hash(cmd.Password);
        User user;
        try
        {
            user = User.Register(email, hash, clock);
        }
        catch (DomainException ex)
        {
            return Error.Domain(ex.Message);
        }

        await users.AddAsync(user, ct);

        var token = jwt.CreateToken(user);
        return new AuthResponse(token.AccessToken, token.ExpiresAtUtc, user.Id, user.Email.Value);
    }
}
