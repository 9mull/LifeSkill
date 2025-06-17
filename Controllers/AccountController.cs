using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using LifeSkill.Web.Models;
using LifeSkill.Web.Services;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace LifeSkill.Web.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IEmailService _emailService;
    private readonly ILogger<AccountController> _logger;
    private const int TOKEN_EXPIRATION_MINUTES = 20;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IEmailService emailService,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailService = emailService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByNameAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Неверный логин или пароль");
                return View(model);
            }

            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                ModelState.AddModelError(string.Empty, "Вы должны подтвердить свой email перед входом.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(user.UserName, model.Password, model.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                return RedirectToLocal(returnUrl);
            }
            ModelState.AddModelError(string.Empty, "Неверный логин или пароль");
        }
        return View(model);
    }

    [HttpGet]
    public IActionResult Register(string returnUrl = null)
    {
        var pendingEmail = TempData.Peek("UserEmail")?.ToString();
        if (!string.IsNullOrEmpty(pendingEmail))
        {
            TempData.Keep("UserEmail");
            TempData.Keep("ConfirmationToken");
            return RedirectToAction(nameof(ConfirmEmail), new { email = pendingEmail });
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (ModelState.IsValid)
        {
            var user = new ApplicationUser 
            { 
                UserName = model.Email, 
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                var confirmationCode = GenerateUniqueToken();
                
                var fullToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                
                TempData["ConfirmationToken"] = fullToken;
                TempData["ConfirmationCode"] = confirmationCode;
                TempData["UserEmail"] = user.Email;
                TempData["UserId"] = user.Id;
                TempData["RegistrationTime"] = DateTime.UtcNow.ToString("O");

                var message = $@"
                    <h2>Добро пожаловать в LifeSkill Safety Training!</h2>
                    <p>Ваш код подтверждения: <strong>{confirmationCode}</strong></p>
                    <p>Код действителен в течение {TOKEN_EXPIRATION_MINUTES} минут.</p>
                    <p>Если вы не регистрировались на нашем сайте, проигнорируйте это сообщение.</p>";

                await _emailService.SendEmailAsync(model.Email, "Подтверждение email", message);

                return RedirectToAction(nameof(ConfirmEmail), new { email = user.Email });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
        return View(model);
    }

    [HttpGet]
    public IActionResult ConfirmEmail(string email)
    {
        var model = new ConfirmEmailViewModel { Email = email };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> ConfirmEmail(ConfirmEmailViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var storedToken = TempData.Peek("ConfirmationToken")?.ToString();
        var storedCode = TempData.Peek("ConfirmationCode")?.ToString();
        var storedEmail = TempData.Peek("UserEmail")?.ToString();
        var userId = TempData.Peek("UserId")?.ToString();
        var registrationTimeStr = TempData.Peek("RegistrationTime")?.ToString();

        if (string.IsNullOrEmpty(storedToken) || string.IsNullOrEmpty(storedEmail) || storedEmail != model.Email)
        {
            ModelState.AddModelError(string.Empty, "Данные регистрации не найдены. Пожалуйста, зарегистрируйтесь заново.");
            return View(model);
        }

        if (DateTime.TryParse(registrationTimeStr, out DateTime registrationTime))
        {
            var timeSinceRegistration = DateTime.UtcNow - registrationTime;
            if (timeSinceRegistration.TotalMinutes > TOKEN_EXPIRATION_MINUTES)
            {
                var existingUser = await _userManager.FindByIdAsync(userId);
                if (existingUser != null)
                {
                    await _userManager.DeleteAsync(existingUser);
                }
                TempData.Clear();
                ModelState.AddModelError(string.Empty, "Срок действия кода подтверждения истек. Пожалуйста, зарегистрируйтесь заново.");
                return RedirectToAction(nameof(Register));
            }
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Пользователь не найден.");
            return View(model);
        }

        if (model.Token != storedCode)
        {
            ModelState.AddModelError(string.Empty, "Неверный код подтверждения.");
            return View(model);
        }

        try
        {
            var result = await _userManager.ConfirmEmailAsync(user, storedToken);
            if (result.Succeeded)
            {
                TempData.Clear();
                
                await _signInManager.SignInAsync(user, isPersistent: false);
                
                TempData["SuccessMessage"] = "Email успешно подтвержден";
                
                return RedirectToAction("Index", "Home");
            }

            _logger.LogError($"Email confirmation failed for user {user.Id}. Errors: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            ModelState.AddModelError(string.Empty, "Ошибка при подтверждении email. Пожалуйста, попробуйте еще раз.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Exception during email confirmation for user {user.Id}: {ex}");
            ModelState.AddModelError(string.Empty, "Произошла внутренняя ошибка при подтверждении email.");
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> CancelRegistration()
    {
        var userId = TempData["UserId"]?.ToString();
        if (!string.IsNullOrEmpty(userId))
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
            }
        }
        
        TempData.Clear();
        
        return RedirectToAction(nameof(Register));
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(HomeController.Index), "Home");
    }

    private IActionResult RedirectToLocal(string returnUrl)
    {
        if (Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        else
        {
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }
    }

    private string GenerateUniqueToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        var random = BitConverter.ToUInt32(bytes, 0);
        var token = (random % 900000 + 100000).ToString();
        
        TempData["ConfirmationCode"] = token;
        
        return token;
    }
} 