using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LifeSkill.Web.Data;
using LifeSkill.Web.Models;

namespace LifeSkill.Web.Controllers;

public class CoursesController : Controller
{
    private readonly ApplicationDbContext _context;

    public CoursesController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var courses = await _context.Courses
            .Include(c => c.Lessons)
            .OrderBy(c => c.OrderIndex)
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
            .Include(c => c.Lessons.OrderBy(l => l.OrderIndex))
            .FirstOrDefaultAsync(c => c.Id == id);

        if (course == null)
        {
            return NotFound();
        }

        return View(course);
    }
} 