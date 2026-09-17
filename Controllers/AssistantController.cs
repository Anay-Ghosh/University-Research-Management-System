using LifeNetAssist.MVC.Data;
using LifeNetAssist.MVC.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace LifeNetAssist.MVC.Controllers
{
    public class AssistantController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly GeminiService _ai;

        public AssistantController(ApplicationDbContext context, GeminiService ai)
        {
            _context = context;
            _ai = ai;
        }

        private int? CurrentUserId() => HttpContext.Session.GetInt32("UserId");

        // GET: /Assistant/Chat
        public IActionResult Chat()
        {
            if (CurrentUserId() == null) return RedirectToAction("Login", "User");
            return View();
        }

        // POST: /Assistant/Ask
        [HttpPost]
        public async Task<IActionResult> Ask([FromBody] AskRequest request)
        {
            var userId = CurrentUserId();
            if (userId == null) return Json(new { reply = "Your session expired. Please log in again." });

            if (request == null || string.IsNullOrWhiteSpace(request.Message))
                return Json(new { reply = "Please type a question." });

            var role = HttpContext.Session.GetString("UserRole") ?? "User";
            var name = HttpContext.Session.GetString("UserName") ?? "User";

            var context = new StringBuilder();
            context.AppendLine("You are the AI research assistant inside URMS (University Research Management System).");
            context.AppendLine("Help with research writing, methodology, literature review, citation formats, and using this system.");
            context.AppendLine("Be concise and practical. Do not write a full thesis for the student; guide them instead.");
            context.AppendLine();
            context.AppendLine("Current user: " + name + " (role: " + role + ")");

            if (role == "Student")
            {
                var proposals = _context.ResearchProposals
                    .Include(p => p.Supervisor)
                    .Where(p => p.StudentId == userId)
                    .ToList();

                if (proposals.Any())
                {
                    context.AppendLine("Their research proposals:");
                    foreach (var p in proposals)
                    {
                        context.AppendLine("- " + p.Title + " [status: " + p.Status + "]"
                            + (p.Supervisor != null ? " supervised by " + p.Supervisor.Name : " (no supervisor yet)"));
                    }
                }
                else
                {
                    context.AppendLine("They have not submitted any proposal yet.");
                }
            }
            else if (role == "Supervisor")
            {
                var count = _context.ResearchProposals.Count(p => p.SupervisorId == userId);
                context.AppendLine("They currently supervise " + count + " research project(s).");
            }

            var reply = await _ai.AskAsync(request.Message, context.ToString());
            return Json(new { reply });
        }

        public class AskRequest
        {
            public string Message { get; set; } = "";
        }
    }
}
