using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LifeSkill.Web.Models;

public class UserSandboxStats
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    [ForeignKey("UserId")]
    public ApplicationUser User { get; set; } = null!;

    public int Attempts { get; set; } = 0;
    public DateTime? LastAttemptAt { get; set; }
    public int TotalCorrect { get; set; } = 0;
} 