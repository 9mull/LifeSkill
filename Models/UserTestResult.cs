using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LifeSkill.Web.Models;

public class UserTestResult
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    [ForeignKey("UserId")]
    public ApplicationUser User { get; set; } = null!;

    [Required]
    public int LessonId { get; set; }
    [ForeignKey("LessonId")]
    public Lesson Lesson { get; set; } = null!;

    public bool IsPassed { get; set; }
    public DateTime PassedAt { get; set; }
} 