using System.Text.RegularExpressions;

namespace TaskFlow.Domain.Users;

public sealed partial record Email
{
    private static readonly Regex Pattern = MyRegex();

    public string Value { get; }

    private Email(string value) => Value = value;

    public static Email Create(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new Common.DomainException("Email is required.");
        }

        var trimmed = raw.Trim().ToLowerInvariant();

        if (trimmed.Length > 254 || !Pattern.IsMatch(trimmed))
        {
            throw new Common.DomainException("Email format is invalid.");
        }

        return new Email(trimmed);
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant)]
    private static partial Regex MyRegex();
}
