using System.ComponentModel.DataAnnotations;

namespace LifeSkill.Web.Models;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Имя обязательно")]
    [Display(Name = "Имя")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Фамилия обязательна")]
    [Display(Name = "Фамилия")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email обязателен")]
    [EmailAddress(ErrorMessage = "Неверный формат email")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Пароль обязателен")]
    [StringLength(100, ErrorMessage = "Пароль должен содержать минимум {2} символов", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Пароль")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Подтверждение пароля")]
    [Compare("Password", ErrorMessage = "Пароли не совпадают")]
    public string ConfirmPassword { get; set; } = string.Empty;
} 