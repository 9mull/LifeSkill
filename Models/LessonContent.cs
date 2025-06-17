using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LifeSkill.Web.Models;

public class LessonContent
{
    [Key]
    public int Id { get; set; }

    [ForeignKey("Lesson")]
    public int LessonId { get; set; }
    public Lesson? Lesson { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;
} 