using FluentAssertions;
using Microsoft.Extensions.Options;
using TaskFlow.Domain.Common;
using TaskFlow.Domain.Users;
using TaskFlow.Infrastructure.Security;
using Xunit;

namespace TaskFlow.Infrastructure.Tests.Security;

public class IdentityPasswordHasherTests
{
    [Fact]
    public void HashAndVerify_RoundTrips()
    {
        var hasher = new IdentityPasswordHasher();

        var hash = hasher.Hash("Password1!");

        hasher.Verify("Password1!", hash).Should().BeTrue();
        hasher.Verify("Wrong!", hash).Should().BeFalse();
    }
}

public class JwtTokenServiceTests
{
    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow { get; } = new(2025, 11, 5, 12, 0, 0, TimeSpan.Zero);
    }

    [Fact]
    public void CreateToken_ReturnsTokenAndExpiry()
    {
        var options = Options.Create(new JwtOptions
        {
            Issuer = "taskflow",
            Audience = "clients",
            SigningKey = "ThisIsASuperSecretSigningKey_32+chars!",
            ExpiryMinutes = 30,
        });
        var clock = new FixedClock();
        var svc = new JwtTokenService(options, clock);
        var user = User.Register(Email.Create("a@b.co"), "h", clock);

        var token = svc.CreateToken(user);

        token.AccessToken.Should().NotBeNullOrWhiteSpace();
        token.ExpiresAtUtc.Should().Be(clock.UtcNow.AddMinutes(30));
    }

    [Fact]
    public void CreateToken_WhenKeyTooShort_Throws()
    {
        var options = Options.Create(new JwtOptions { SigningKey = "short" });
        var svc = new JwtTokenService(options, new FixedClock());
        var user = User.Register(Email.Create("a@b.co"), "h", new FixedClock());

        var act = () => svc.CreateToken(user);
        act.Should().Throw<InvalidOperationException>();
    }
}
