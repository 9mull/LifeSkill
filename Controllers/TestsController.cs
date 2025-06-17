using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LifeSkill.Web.Data;
using LifeSkill.Web.Models;
using Microsoft.AspNetCore.Identity;

namespace LifeSkill.Web.Controllers;

public class TestsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public TestsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var courses = await _context.Courses
            .Include(c => c.Lessons)
            .OrderBy(s => s.OrderIndex)
            .ToListAsync();
        return View(courses);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var course = await _context.Courses
            .Include(s => s.Lessons.OrderBy(l => l.OrderIndex))
            .ThenInclude(l => l.TestQuestions)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (course == null)
        {
            return NotFound();
        }

        var userId = _userManager.GetUserId(User);
        var passedLessons = new HashSet<int>();
        if (User.Identity?.IsAuthenticated == true)
        {
            passedLessons = _context.UserTestResults
                .Where(r => r.UserId == userId && r.IsPassed)
                .Select(r => r.LessonId)
                .ToHashSet();
        }
        ViewBag.PassedLessons = passedLessons;

        return View(course);
    }
} 