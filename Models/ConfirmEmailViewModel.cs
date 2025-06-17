using System.ComponentModel.DataAnnotations;

namespace LifeSkill.Web.Models;

public class ConfirmEmailViewModel
{
    [Required(ErrorMessage = "Введите код подтверждения")]
    [Display(Name = "Код подтверждения")]
    public string Token { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
} 