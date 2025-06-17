using Microsoft.AspNetCore.Identity;

namespace LifeSkill.Web.Models;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
    public string? ProfilePicture { get; set; }
    public bool ExamPassed { get; set; } = false;

    private new bool PhoneNumberConfirmed { get; set; }
    private new bool TwoFactorEnabled { get; set; }
    private new bool LockoutEnabled { get; set; }
    private new DateTimeOffset? LockoutEnd { get; set; }
    private new int AccessFailedCount { get; set; }
    private new string? SecurityStamp { get; set; }
    private new string? ConcurrencyStamp { get; set; }
    private new string? PhoneNumber { get; set; }

    private string? _email;
    public override string? Email
    {
        get => _email;
        set
        {
            _email = value;
            NormalizedEmail = value?.ToUpperInvariant();
        }
    }

    private string? _userName;
    public override string? UserName
    {
        get => _userName;
        set
        {
            _userName = value;
            NormalizedUserName = value?.ToUpperInvariant();
        }
    }
} 