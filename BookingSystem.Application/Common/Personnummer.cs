// Backend purpose: Validates Swedish personal identity and coordination numbers and normalises them to ÅÅÅÅMMDD-XXXX.
using System;
using System.Linq;

namespace BookingSystem.Application.Common;

public static class Personnummer
{
    public const string FormatMessage =
        "Personnumret är ogiltigt. Ange ÅÅÅÅMMDD-XXXX eller ÅÅMMDD-XXXX med korrekt kontrollsiffra.";

    // Accepts ÅÅÅÅMMDD-XXXX, ÅÅÅÅMMDDXXXX, ÅÅMMDD-XXXX, ÅÅMMDDXXXX and ÅÅMMDD+XXXX (100 years or older).
    // Coordination numbers (day + 60) are accepted. Returns the 12-digit form with a hyphen.
    public static bool TryNormalize(string? value, DateTime today, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(value)) return false;

        var trimmed = value.Trim();
        var isCentenarian = trimmed.Contains('+');
        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        var allowed = trimmed.All(c => char.IsDigit(c) || c == '-' || c == '+' || c == ' ');
        if (!allowed || trimmed.Count(c => c == '-' || c == '+') > 1) return false;

        int year;
        string tenDigits;
        if (digits.Length == 12)
        {
            if (isCentenarian) return false;
            year = int.Parse(digits[..4]);
            tenDigits = digits[2..];
        }
        else if (digits.Length == 10)
        {
            tenDigits = digits;
            var shortYear = int.Parse(digits[..2]);
            var century = today.Year / 100 * 100;
            year = century + shortYear;
            if (year > today.Year) year -= 100;
            if (isCentenarian) year -= 100;
        }
        else
        {
            return false;
        }

        var month = int.Parse(tenDigits.Substring(2, 2));
        var rawDay = int.Parse(tenDigits.Substring(4, 2));
        var day = rawDay > 60 ? rawDay - 60 : rawDay;

        if (year < 1850 || month is < 1 or > 12 || day < 1 || day > DateTime.DaysInMonth(year, month)) return false;
        if (rawDay <= 60 && new DateTime(year, month, day) > today) return false;
        if (!LuhnIsValid(tenDigits)) return false;

        normalized = $"{year:D4}{tenDigits.Substring(2, 4)}-{tenDigits[6..]}";
        return true;
    }

    public static bool IsValid(string? value) => TryNormalize(value, DateTime.Today, out _);

    private static bool LuhnIsValid(string tenDigits)
    {
        var sum = 0;
        for (var i = 0; i < tenDigits.Length; i++)
        {
            var digit = tenDigits[i] - '0';
            var value = i % 2 == 0 ? digit * 2 : digit;
            sum += value > 9 ? value - 9 : value;
        }
        return sum % 10 == 0;
    }
}
