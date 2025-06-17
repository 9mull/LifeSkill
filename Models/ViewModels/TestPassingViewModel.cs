using System.Collections.Generic;

namespace LifeSkill.Web.Models.ViewModels;

public class TestPassingViewModel
{
    public int LessonId { get; set; }
    public string LessonTitle { get; set; } = string.Empty;
    public List<QuestionItem> Questions { get; set; } = new();
    public Dictionary<int, int>? UserAnswers { get; set; } 
    public int TotalQuestions => Questions.Count;
    public int PassingScore => (int)System.Math.Ceiling(0.8 * TotalQuestions);
    public int CourseId { get; set; }

    public class QuestionItem
    {
        public int Id { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string Option1 { get; set; } = string.Empty;
        public string Option2 { get; set; } = string.Empty;
        public string Option3 { get; set; } = string.Empty;
        public string Option4 { get; set; } = string.Empty;
    }
} 