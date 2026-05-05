using FluentAssertions;
using TaskFlow.Domain.Common;
using TaskFlow.Domain.Users;
using Xunit;

namespace TaskFlow.Domain.Tests.Users;

public class EmailTests
{
    [Theory]
    [InlineData("a@b.co")]
    [InlineData("Demo@TaskFlow.dev")]
    public void Create_WhenValid_NormalizesAndReturnsEmail(string raw)
    {
        var email = Email.Create(raw);

        email.Value.Should().Be(raw.Trim().ToLowerInvariant());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    [InlineData("missing@dot")]
    [InlineData("two@@at.com")]
    public void Create_WhenInvalid_Throws(string raw)
    {
        var act = () => Email.Create(raw);

        act.Should().Throw<DomainException>();
    }
}

public class UserTests
{
    private static readonly IClock Clock = new FixedClock(new DateTimeOffset(2025, 11, 5, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Register_WhenValid_CreatesUserWithGeneratedId()
    {
        var user = User.Register(Email.Create("demo@taskflow.dev"), "hash", Clock);

        user.Id.Should().NotBeEmpty();
        user.Email.Value.Should().Be("demo@taskflow.dev");
        user.PasswordHash.Should().Be("hash");
        user.CreatedAtUtc.Should().Be(Clock.UtcNow);
    }

    [Fact]
    public void Register_WhenPasswordHashEmpty_Throws()
    {
        var act = () => User.Register(Email.Create("a@b.co"), "  ", Clock);

        act.Should().Throw<DomainException>();
    }
}
