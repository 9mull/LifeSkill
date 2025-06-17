using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LifeSkill.Web.Models;

public class Lesson
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    public int OrderIndex { get; set; }

    [ForeignKey("Course")]
    public int CourseId { get; set; }
    public Course? Course { get; set; }

    public LessonContent? ContentBlock { get; set; }

    public ICollection<TestQuestion>? TestQuestions { get; set; }
} 