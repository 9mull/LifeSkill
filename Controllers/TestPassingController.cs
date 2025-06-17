using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using LifeSkill.Web.Data;
using LifeSkill.Web.Models.ViewModels;
using LifeSkill.Web.Models;

namespace LifeSkill.Web.Controllers;

[Authorize]
public class TestPassingController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    public TestPassingController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Start(int lessonId)
    {
        var lesson = await _context.Lessons.Include(l => l.Course).FirstOrDefaultAsync(l => l.Id == lessonId);
        if (lesson == null) return NotFound();
        var questions = await _context.TestQuestions
            .Where(q => q.LessonId == lessonId)
            .Select(q => new TestPassingViewModel.QuestionItem
            {
                Id = q.Id,
                QuestionText = q.QuestionText,
                Option1 = q.Option1,
                Option2 = q.Option2,
                Option3 = q.Option3,
                Option4 = q.Option4
            })
            .ToListAsync();
        var vm = new TestPassingViewModel
        {
            LessonId = lessonId,
            LessonTitle = lesson.Title,
            Questions = questions,
            CourseId = lesson.CourseId
        };
        ViewBag.CourseId = lesson.CourseId;
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Start(int lessonId, Dictionary<int, int> userAnswers)
    {
        var lesson = await _context.Lessons.Include(l => l.Course).FirstOrDefaultAsync(l => l.Id == lessonId);
        var questions = await _context.TestQuestions
            .Where(q => q.LessonId == lessonId)
            .ToListAsync();

        if (userAnswers == null || userAnswers.Count != questions.Count)
        {
            ViewBag.Error = "Пожалуйста, ответьте на все вопросы теста.";
            var vm = new TestPassingViewModel
            {
                LessonId = lessonId,
                LessonTitle = lesson?.Title ?? "Тест",
                Questions = questions.Select(q => new TestPassingViewModel.QuestionItem
                {
                    Id = q.Id,
                    QuestionText = q.QuestionText,
                    Option1 = q.Option1,
                    Option2 = q.Option2,
                    Option3 = q.Option3,
                    Option4 = q.Option4
                }).ToList(),
                UserAnswers = userAnswers,
                CourseId = lesson?.CourseId ?? 0
            };
            ViewBag.CourseId = lesson?.CourseId ?? 0;
            return View(vm);
        }

        int correct = 0;
        foreach (var q in questions)
        {
            if (userAnswers.TryGetValue(q.Id, out int answer) && answer == q.CorrectOption)
                correct++;
        }
        int total = questions.Count;
        int passing = (int)Math.Ceiling(0.8 * total);
        bool passed = correct >= passing;
        ViewBag.Correct = correct;
        ViewBag.Total = total;
        ViewBag.Passing = passing;
        ViewBag.Passed = passed;
        ViewBag.LessonId = lessonId;
        ViewBag.CourseId = lesson?.CourseId ?? 0;

        if (passed)
        {
            var userId = _userManager.GetUserId(User);
            var existing = await _context.UserTestResults.FirstOrDefaultAsync(r => r.UserId == userId && r.LessonId == lessonId);
            if (existing != null)
            {
                existing.IsPassed = true;
                existing.PassedAt = DateTime.UtcNow;
                _context.UserTestResults.Update(existing);
            }
            else
            {
                var result = new UserTestResult
                {
                    UserId = userId!,
                    LessonId = lessonId,
                    IsPassed = true,
                    PassedAt = DateTime.UtcNow
                };
                await _context.UserTestResults.AddAsync(result);
            }
            await _context.SaveChangesAsync();
        }

        return View("Result");
    }
} 