using System;
namespace BookingSystem.Application.Service.Implementation;
public static class PasswordVerification
{
    public static bool Matches(string? password,string? hash)
    {
        if(string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash)) return false;
        try { return BCrypt.Net.BCrypt.Verify(password,hash); }
        catch(BCrypt.Net.SaltParseException) { return false; }
        catch(ArgumentException) { return false; }
        catch(FormatException) { return false; }
        catch(IndexOutOfRangeException) { return false; }
    }
}
