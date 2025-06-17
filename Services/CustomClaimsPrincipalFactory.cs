using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using LifeSkill.Web.Models;
using System.Security.Claims;

namespace LifeSkill.Web.Services;

public class CustomClaimsPrincipalFactory : IUserClaimsPrincipalFactory<ApplicationUser>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IOptions<IdentityOptions> _optionsAccessor;

    public CustomClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        IOptions<IdentityOptions> optionsAccessor)
    {
        _userManager = userManager;
        _optionsAccessor = optionsAccessor;
    }

    public async Task<ClaimsPrincipal> CreateAsync(ApplicationUser user)
    {
        var identity = new ClaimsIdentity(IdentityConstants.ApplicationScheme);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));
        identity.AddClaim(new Claim(ClaimTypes.Name, user.UserName ?? string.Empty));
        identity.AddClaim(new Claim(ClaimTypes.Email, user.Email ?? string.Empty));

        return new ClaimsPrincipal(identity);
    }
} 