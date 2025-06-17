using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LifeSkill.Web.Models;

public class TestQuestion
{
    [Key]
    public int Id { get; set; }

    [ForeignKey("Lesson")]
    public int LessonId { get; set; }
    public Lesson Lesson { get; set; }

    [Required]
    public string QuestionText { get; set; } = string.Empty;
    [Required]
    public string Option1 { get; set; } = string.Empty;
    [Required]
    public string Option2 { get; set; } = string.Empty;
    [Required]
    public string Option3 { get; set; } = string.Empty;
    [Required]
    public string Option4 { get; set; } = string.Empty;
    [Required]
    public int CorrectOption { get; set; } // 1-4
} 