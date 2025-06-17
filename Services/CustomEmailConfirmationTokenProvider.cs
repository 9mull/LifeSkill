using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using LifeSkill.Web.Models;

namespace LifeSkill.Web.Services;

public class CustomEmailConfirmationTokenProvider<TUser> : IUserTwoFactorTokenProvider<TUser> where TUser : ApplicationUser
{
    private readonly IDataProtectionProvider _dataProtectionProvider;
    private readonly string _purpose = "EmailConfirmation";

    public CustomEmailConfirmationTokenProvider(IDataProtectionProvider dataProtectionProvider)
    {
        _dataProtectionProvider = dataProtectionProvider;
    }

    public async Task<string> GenerateAsync(string purpose, UserManager<TUser> manager, TUser user)
    {
        var protector = _dataProtectionProvider.CreateProtector(_purpose);
        var token = $"{user.Id}|{user.Email}|{DateTime.UtcNow.Ticks}";
        return protector.Protect(token);
    }

    public async Task<bool> ValidateAsync(string purpose, string token, UserManager<TUser> manager, TUser user)
    {
        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        var protector = _dataProtectionProvider.CreateProtector(_purpose);
        try
        {
            var unprotectedToken = protector.Unprotect(token);
            var parts = unprotectedToken.Split('|');
            if (parts.Length != 3)
            {
                return false;
            }

            var userId = parts[0];
            var email = parts[1];
            var timestamp = long.Parse(parts[2]);
            var tokenDate = new DateTime(timestamp, DateTimeKind.Utc);

            // Token is valid if:
            // 1. User ID matches
            // 2. Email matches
            // 3. Token is not older than 24 hours
            return userId == user.Id &&
                   email == user.Email &&
                   (DateTime.UtcNow - tokenDate).TotalHours <= 24;
        }
        catch
        {
            return false;
        }
    }

    public Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<TUser> manager, TUser user)
    {
        return Task.FromResult(true);
    }
} 