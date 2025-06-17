using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LifeSkill.Web.Data;
using LifeSkill.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;
using iText.Kernel.Geom;
using iText.IO.Image;
using iText.Layout.Borders;
using MimeKit;
using MailKit.Net.Smtp;
using Path = System.IO.Path;
using Microsoft.Extensions.Configuration;
using iTextSharp.text.pdf;
using iTextSharp.text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Drawing;
using QuestPDF.Elements;
using QuestPDF.Previewer;

namespace LifeSkill.Web.Controllers;

[Authorize]
public class ExamController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;

    public ExamController(
        ApplicationDbContext context, 
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration)
    {
        _context = context;
        _userManager = userManager;
        _configuration = configuration;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        if (user.ExamPassed)
        {
            return RedirectToAction("Success");
        }

        var userId = user.Id;
        var courses = await _context.Courses
            .Include(c => c.Lessons)
            .ToListAsync();

        var passedLessonIds = await _context.UserTestResults
            .Where(r => r.UserId == userId && r.IsPassed)
            .Select(r => r.LessonId)
            .ToListAsync();

        var allLessonsPassed = courses.All(course => course.Lessons.All(lesson => passedLessonIds.Contains(lesson.Id)));

        if (!allLessonsPassed)
        {
            ViewBag.Message = "Пройдите все курсы, чтобы получить доступ к итоговому экзамену и сертификату о прохождении LifeSkill.";
        }

        return View();
    }

    public class ExamQuestionViewModel
    {
        public int Id { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new List<string>();
        public int CorrectOption { get; set; } // не передавать на клиент
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Start()
    {
        var questions = await _context.TestQuestions
            .OrderBy(q => Guid.NewGuid())
            .Take(20)
            .ToListAsync();
        var viewModel = questions.Select(q => new ExamQuestionViewModel
        {
            Id = q.Id,
            QuestionText = q.QuestionText,
            Options = new List<string> { q.Option1, q.Option2, q.Option3, q.Option4 }
        }).ToList();
        return View("ExamSession", viewModel);
    }

    public class ExamSubmissionModel
    {
        public List<int> QuestionIds { get; set; } = new List<int>();
        public List<int> UserAnswers { get; set; } = new List<int>();
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> SubmitExam([FromBody] ExamSubmissionModel model)
    {
        try
        {
            if (model == null)
            {
                return BadRequest(new { error = "Модель данных пуста" });
            }

            if (model.QuestionIds == null || model.QuestionIds.Count == 0)
            {
                return BadRequest(new { error = "Список вопросов пуст" });
            }

            if (model.UserAnswers == null || model.UserAnswers.Count == 0)
            {
                return BadRequest(new { error = "Список ответов пуст" });
            }

            if (model.QuestionIds.Count != model.UserAnswers.Count)
            {
                return BadRequest(new { error = "Количество вопросов не совпадает с количеством ответов" });
            }

            int correct = 0;
            for (int i = 0; i < model.QuestionIds.Count; i++)
            {
                var question = await _context.TestQuestions.FindAsync(model.QuestionIds[i]);
                if (question != null && model.UserAnswers[i] == question.CorrectOption)
                    correct++;
            }

            bool isSuccess = correct == model.QuestionIds.Count && correct == 20;

            if (isSuccess)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    user.ExamPassed = true;
                    _context.Update(user);
                    await _context.SaveChangesAsync();
                    try
                    {
                        var pdfBytes = GenerateCertificate(user.FirstName + " " + user.LastName);
                        await SendCertificateEmail(user.Email, pdfBytes);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка при генерации сертификата: {ex.Message}");
                    }
                }
                TempData["ExamSuccess"] = true;
                return Json(new { success = true });
            }
            else
            {
                TempData["ExamSuccess"] = false;
                return Json(new {success = false });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Внутренняя ошибка сервера", details = ex.Message });
        }
    }

    private byte[] GenerateCertificate(string studentName)
    {
        try
        {
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
            var date = DateTime.Now.ToString("dd.MM.yyyy");
            var bytes = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0);
                    page.PageColor(Colors.Blue.Lighten4);
                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(16));

                    page.Content().Element(c =>
                    {
                        c.Padding(50)
                         .Background(Colors.White)
                         .Border(10).BorderColor(Colors.Purple.Medium)
                         .PaddingVertical(40)
                         .PaddingHorizontal(60)
                         .Column(col =>
                         {
                             col.Spacing(25);
                             col.Item().Row(row =>
                             {
                                 row.RelativeItem().LineHorizontal(4).LineColor(Colors.Purple.Medium);
                             });
                             col.Item().AlignCenter().Text("СЕРТИФИКАТ")
                                .FontSize(44).Bold().FontColor(Colors.Purple.Darken2);
                             col.Item().AlignCenter().Text("о прохождении итогового экзамена")
                                .FontSize(20).FontColor(Colors.Grey.Darken2).Italic();
                             col.Item().AlignCenter().Text("★ ★ ★")
                                .FontSize(32).FontColor(Colors.Orange.Medium);
                             col.Item().AlignCenter().Text("Настоящим удостоверяется, что")
                                .FontSize(20);
                             col.Item().AlignCenter().Text(studentName)
                                .FontSize(32).Bold().FontColor(Colors.Blue.Darken2).Underline();
                             col.Item().AlignCenter().Text("успешно прошел(а) итоговый экзамен")
                                .FontSize(20);
                            col.Item().AlignCenter().Text("по курсам")
                                .FontSize(20);
                             col.Item().AlignCenter().Text("LifeSkill Safety Training")
                                .FontSize(26).Bold().FontColor(Colors.Green.Darken1);
                             col.Item().PaddingVertical(50);
                             col.Item().AlignCenter().Text($"Дата выдачи: {date}")
                                .FontSize(16).FontColor(Colors.Grey.Darken2);
                             col.Item().Row(row =>
                             {
                                 row.RelativeItem().LineHorizontal(4).LineColor(Colors.Purple.Medium);
                             });
                         });
                    });
                });
            }).GeneratePdf();
            return bytes;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при генерации сертификата (QuestPDF): {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    private async Task SendCertificateEmail(string email, byte[] pdfBytes)
    {
        try
        {
            
            var smtpSettings = _configuration.GetSection("SmtpSettings");
            var port = smtpSettings["Port"] ?? "465";
            var useSsl = smtpSettings["UseSsl"] ?? "true";
            

            var message = new MimeKit.MimeMessage();
            message.From.Add(new MimeKit.MailboxAddress(
                smtpSettings["FromName"] ?? "LifeSkill", 
                smtpSettings["FromEmail"] ?? "lifeskilldr@mail.ru"));
            message.To.Add(new MimeKit.MailboxAddress("", email));
            message.Subject = "Сертификат о прохождении LifeSkill";


            var builder = new MimeKit.BodyBuilder
            {
                HtmlBody = @"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #2ecc71; margin-bottom: 20px;'>Поздравляем с успешным прохождением экзамена!</h2>
                        <p style='color: #333; line-height: 1.6; margin-bottom: 20px;'>
                            Вы успешно прошли итоговый экзамен по курсам LifeSkill Safety Training.
                            Ваш сертификат прикреплен к этому письму.
                        </p>
                        <p style='color: #666; font-size: 14px;'>
                            Вы можете скачать сертификат в формате PDF и распечатать его.
                        </p>
                        <div style='margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; color: #999; font-size: 12px;'>
                            С уважением,<br>
                            Команда LifeSkill
                        </div>
                    </div>"
            };

            builder.Attachments.Add("certificate.pdf", pdfBytes, new MimeKit.ContentType("application", "pdf"));
            message.Body = builder.ToMessageBody();

            using (var client = new MailKit.Net.Smtp.SmtpClient())
            {
                try
                {
                    await client.ConnectAsync(
                        smtpSettings["Server"] ?? "smtp.mail.ru",
                        int.Parse(port),
                        bool.Parse(useSsl));

                    await client.AuthenticateAsync(
                        smtpSettings["Username"] ?? "",
                        smtpSettings["Password"] ?? "");

                    await client.SendAsync(message);

                    await client.DisconnectAsync(true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при работе с SMTP: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    throw;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Критическая ошибка при отправке сертификата: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Success()
    {
        var user = await _userManager.GetUserAsync(User);
        ViewBag.IsSuccess = TempData["ExamSuccess"] as bool? ?? false;
        ViewBag.ExamPassed = user?.ExamPassed ?? false;
        return View();
    }
} 