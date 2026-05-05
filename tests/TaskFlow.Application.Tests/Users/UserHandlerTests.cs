using FluentAssertions;
using Moq;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Common;
using TaskFlow.Application.Users;
using TaskFlow.Domain.Common;
using TaskFlow.Domain.Users;
using Xunit;

namespace TaskFlow.Application.Tests.Users;

public class RegisterUserHandlerTests
{
    private static readonly IClock Clock = new FixedClock(new DateTimeOffset(2025, 11, 5, 12, 0, 0, TimeSpan.Zero));

    private static (RegisterUserHandler handler, Mock<IUserRepository> users, Mock<IPasswordHasher> hasher, Mock<IJwtTokenService> jwt) Build()
    {
        var users = new Mock<IUserRepository>();
        var hasher = new Mock<IPasswordHasher>();
        var jwt = new Mock<IJwtTokenService>();
        hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("HASH");
        jwt.Setup(j => j.CreateToken(It.IsAny<User>()))
            .Returns(new JwtTokenResult("token", Clock.UtcNow.AddHours(1)));
        var handler = new RegisterUserHandler(users.Object, hasher.Object, jwt.Object, Clock, new RegisterUserValidator());
        return (handler, users, hasher, jwt);
    }

    [Fact]
    public async Task Register_WhenNewEmail_ReturnsAuthResponse()
    {
        var (handler, users, _, _) = Build();
        users.Setup(u => u.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var result = await handler.HandleAsync(new RegisterUserCommand("demo@taskflow.dev", "Password1!"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AccessToken.Should().Be("token");
        users.Verify(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Register_WhenEmailExists_ReturnsConflict()
    {
        var (handler, users, _, _) = Build();
        var existing = User.Register(Email.Create("demo@taskflow.dev"), "x", Clock);
        users.Setup(u => u.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var result = await handler.HandleAsync(new RegisterUserCommand("demo@taskflow.dev", "Password1!"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task Register_WhenPasswordTooShort_ReturnsValidation()
    {
        var (handler, _, _, _) = Build();

        var result = await handler.HandleAsync(new RegisterUserCommand("demo@taskflow.dev", "short"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Type.Should().Be(ErrorType.Validation);
    }
}

public class LoginUserHandlerTests
{
    private static readonly IClock Clock = new FixedClock(new DateTimeOffset(2025, 11, 5, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task Login_WhenValid_ReturnsToken()
    {
        var users = new Mock<IUserRepository>();
        var hasher = new Mock<IPasswordHasher>();
        var jwt = new Mock<IJwtTokenService>();
        var user = User.Register(Email.Create("demo@taskflow.dev"), "HASH", Clock);
        users.Setup(u => u.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);
        hasher.Setup(h => h.Verify("Password1!", "HASH")).Returns(true);
        jwt.Setup(j => j.CreateToken(user)).Returns(new JwtTokenResult("token", Clock.UtcNow.AddHours(1)));

        var handler = new LoginUserHandler(users.Object, hasher.Object, jwt.Object, new LoginUserValidator());

        var result = await handler.HandleAsync(new LoginUserCommand("demo@taskflow.dev", "Password1!"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task Login_WhenWrongPassword_ReturnsUnauthorized()
    {
        var users = new Mock<IUserRepository>();
        var hasher = new Mock<IPasswordHasher>();
        var user = User.Register(Email.Create("demo@taskflow.dev"), "HASH", Clock);
        users.Setup(u => u.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);
        hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        var handler = new LoginUserHandler(users.Object, hasher.Object, Mock.Of<IJwtTokenService>(), new LoginUserValidator());

        var result = await handler.HandleAsync(new LoginUserCommand("demo@taskflow.dev", "Wrong1!!"), CancellationToken.None);

        result.Error!.Type.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Login_WhenUserMissing_ReturnsUnauthorized()
    {
        var users = new Mock<IUserRepository>();
        users.Setup(u => u.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        var handler = new LoginUserHandler(users.Object, Mock.Of<IPasswordHasher>(), Mock.Of<IJwtTokenService>(), new LoginUserValidator());

        var result = await handler.HandleAsync(new LoginUserCommand("nope@taskflow.dev", "Password1!"), CancellationToken.None);

        result.Error!.Type.Should().Be(ErrorType.Unauthorized);
    }
}
