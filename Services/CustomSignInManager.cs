using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using LifeSkill.Web.Models;
using System.Security.Claims;

namespace LifeSkill.Web.Services;

public class CustomSignInManager : SignInManager<ApplicationUser>
{
    public CustomSignInManager(
        UserManager<ApplicationUser> userManager,
        IHttpContextAccessor contextAccessor,
        IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
        IOptions<IdentityOptions> optionsAccessor,
        ILogger<SignInManager<ApplicationUser>> logger,
        IAuthenticationSchemeProvider schemes,
        IUserConfirmation<ApplicationUser> confirmation)
        : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
    {
    }

    public override async Task<SignInResult> PasswordSignInAsync(string userName, string password, bool isPersistent, bool lockoutOnFailure)
    {
        var user = await UserManager.FindByNameAsync(userName);
        if (user == null)
        {
            return SignInResult.Failed;
        }

        return await PasswordSignInAsync(user, password, isPersistent, lockoutOnFailure);
    }

    public override async Task<ClaimsPrincipal> CreateUserPrincipalAsync(ApplicationUser user)
    {
        var identity = new ClaimsIdentity(IdentityConstants.ApplicationScheme);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));
        identity.AddClaim(new Claim(ClaimTypes.Name, user.UserName ?? string.Empty));
        identity.AddClaim(new Claim(ClaimTypes.Email, user.Email ?? string.Empty));

        return new ClaimsPrincipal(identity);
    }
} 