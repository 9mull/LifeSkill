using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LifeSkill.Web.Data;
using LifeSkill.Web.Models;
using System.Security.Claims;

namespace LifeSkill.Web.Controllers;

[Authorize]
public class SandboxController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private const int SandboxQuestionCount = 10;
    private static readonly TimeSpan SandboxCooldown = TimeSpan.FromHours(1);

    public SandboxController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        var stats = await _context.UserSandboxStats.FirstOrDefaultAsync(s => s.UserId == userId);
        bool canStart = true;
        TimeSpan? timeLeft = null;
        if (stats?.LastAttemptAt != null)
        {
            var since = DateTime.UtcNow - stats.LastAttemptAt.Value;
            if (since < SandboxCooldown)
            {
                canStart = false;
                timeLeft = SandboxCooldown - since;
            }
        }
        ViewBag.CanStart = canStart;
        ViewBag.TimeLeft = timeLeft;
        ViewBag.Attempts = stats?.Attempts ?? 0;
        ViewBag.TotalCorrect = stats?.TotalCorrect ?? 0;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Start()
    {
        var userId = _userManager.GetUserId(User);
        var stats = await _context.UserSandboxStats.FirstOrDefaultAsync(s => s.UserId == userId);
        if (stats?.LastAttemptAt != null && DateTime.UtcNow - stats.LastAttemptAt.Value < SandboxCooldown)
        {
            return RedirectToAction("Index");
        }
        var questions = await _context.TestQuestions
            .OrderBy(q => Guid.NewGuid())
            .Take(SandboxQuestionCount)
            .ToListAsync();
        return View("Practice", questions);
    }

    [HttpPost]
    public async Task<IActionResult> Practice(Dictionary<int, int> userAnswers)
    {
        var userId = _userManager.GetUserId(User);
        var questions = await _context.TestQuestions
            .Where(q => userAnswers.Keys.Contains(q.Id))
            .ToListAsync();
        int correct = 0;
        foreach (var q in questions)
        {
            if (userAnswers.TryGetValue(q.Id, out int answer) && answer == q.CorrectOption)
                correct++;
        }
        var stats = await _context.UserSandboxStats.FirstOrDefaultAsync(s => s.UserId == userId);
        if (stats == null)
        {
            stats = new UserSandboxStats
            {
                UserId = userId!,
                Attempts = 1,
                LastAttemptAt = DateTime.UtcNow,
                TotalCorrect = correct
            };
            await _context.UserSandboxStats.AddAsync(stats);
        }
        else
        {
            stats.Attempts++;
            stats.LastAttemptAt = DateTime.UtcNow;
            stats.TotalCorrect += correct;
            _context.UserSandboxStats.Update(stats);
        }
        await _context.SaveChangesAsync();
        ViewBag.Correct = correct;
        ViewBag.Total = questions.Count;
        return View("Result");
    }
} 