using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LifeSkill.Web.Data;
using LifeSkill.Web.Models;
using LifeSkill.Web.Services;
using System.Security.Claims;

namespace LifeSkill.Web.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public ProfileController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext context,
        IWebHostEnvironment environment)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _environment = environment;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        var courses = await _context.Courses
            .Include(c => c.Lessons)
            .OrderBy(c => c.OrderIndex)
            .ToListAsync();

        var userId = user.Id;
        var passedLessonIds = await _context.UserTestResults
            .Where(r => r.UserId == userId && r.IsPassed)
            .Select(r => r.LessonId)
            .ToListAsync();
        var passedLessonSet = new HashSet<int>(passedLessonIds);

        var courseProgresses = courses.Select(course => new CourseProgress
        {
            CourseId = course.Id,
            CourseName = course.Title,
            ImageUrl = course.ImageUrl,
            CompletedLessons = course.Lessons.Count(l => passedLessonSet.Contains(l.Id)),
            TotalLessons = course.Lessons.Count
        }).ToList();

        int completedCourses = courseProgresses.Count(c => c.TotalLessons > 0 && c.CompletedLessons == c.TotalLessons);

        var model = new ProfileViewModel
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email ?? string.Empty,
            CurrentProfilePicture = user.ProfilePicture,
            CourseProgresses = courseProgresses,
            CompletedCourses = completedCourses,
            TotalCourses = courses.Count
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateProfile(ProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        user.FirstName = model.FirstName;
        user.LastName = model.LastName;

        if (Request.Form["RemovePhoto"] == "true")
        {
            if (!string.IsNullOrEmpty(user.ProfilePicture))
            {
                var filePath = Path.Combine(_environment.WebRootPath, user.ProfilePicture.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);
                user.ProfilePicture = null;
            }
        }

        if (model.ProfilePicture != null)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(model.ProfilePicture.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError("ProfilePicture", "Разрешены только изображения в форматах JPG и PNG");
                return View("Index", model);
            }

            var fileName = $"{Guid.NewGuid()}{extension}";
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "profiles");
            
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            if (!string.IsNullOrEmpty(user.ProfilePicture))
            {
                var oldFilePath = Path.Combine(_environment.WebRootPath, user.ProfilePicture.TrimStart('/'));
                if (System.IO.File.Exists(oldFilePath))
                {
                    System.IO.File.Delete(oldFilePath);
                }
            }

            var filePath = Path.Combine(uploadsFolder, fileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await model.ProfilePicture.CopyToAsync(fileStream);
            }

            user.ProfilePicture = $"/uploads/profiles/{fileName}";
        }

        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            TempData["StatusMessage"] = "Профиль успешно обновлен";
        }
        else
        {
            ModelState.AddModelError("", "Произошла ошибка при обновлении профиля");
        }

        return RedirectToAction(nameof(Index));
    }
} 