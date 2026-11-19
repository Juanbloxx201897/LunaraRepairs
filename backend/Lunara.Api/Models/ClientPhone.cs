namespace Lunara.Api.Models;

public static class ClientPhone
{
    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        var digits = new string(trimmed.Where(character => character is >= '0' and <= '9').ToArray());
        if (!trimmed.StartsWith('+') || digits.Length is < 8 or > 15 ||
            digits[0] == '0' || trimmed.Skip(1).Any(character =>
                character is < '0' or > '9' && character is not (' ' or '-' or '(' or ')')))
            return null;

        return $"+{digits}";
    }
}
