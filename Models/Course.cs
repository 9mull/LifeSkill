using System.ComponentModel.DataAnnotations;

namespace LifeSkill.Web.Models;

public class Course
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public int OrderIndex { get; set; }
    
    public string? ImageUrl { get; set; }
    
    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
} 