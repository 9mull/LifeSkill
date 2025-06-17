using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace LifeSkill.Web.Models;

public class ProfileViewModel
{
    [Required(ErrorMessage = "Имя обязательно")]
    [Display(Name = "Имя")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Фамилия обязательна")]
    [Display(Name = "Фамилия")]
    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? CurrentProfilePicture { get; set; }

    [Display(Name = "Фото профиля")]
    public IFormFile? ProfilePicture { get; set; }

    public int CompletedCourses { get; set; } = 0;
    public int TotalCourses { get; set; } = 0;
    public List<CourseProgress> CourseProgresses { get; set; } = new();
}

public class CourseProgress
{
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int CompletedLessons { get; set; }
    public int TotalLessons { get; set; }
} 