using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LifeSkill.Web.Data;

namespace LifeSkill.Web.Controllers;

public class LessonsController : Controller
{
    private readonly ApplicationDbContext _context;
    public LessonsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Details(int id)
    {
        var lesson = await _context.Lessons
            .Include(l => l.Course)
            .Include(l => l.ContentBlock)
            .FirstOrDefaultAsync(l => l.Id == id);
            
        if (lesson == null) return NotFound();
        return View(lesson);
    }
} 