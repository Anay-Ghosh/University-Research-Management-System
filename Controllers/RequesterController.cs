using LifeNetAssist.MVC.Data;
using LifeNetAssist.MVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LifeNetAssist.MVC.Controllers
{
    public class RequesterController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RequesterController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Requester/Dashboard
        public IActionResult Dashboard()
        {
            var role = HttpContext.Session.GetString("UserRole");
            var userId = HttpContext.Session.GetInt32("UserId");

            if (role != "Requester" || userId == null)
            {
                return RedirectToAction("Login", "User");
            }

            // Get all requests for this user
            var requests = _context.HelpRequests
                .Where(r => r.RequesterId == userId)
                .ToList();

            return View(requests);
        }

        // GET: /Requester/Create
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(HelpRequest request)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "User");

            if (!ModelState.IsValid)
                return View(request); // keep showing form if invalid

            request.RequesterId = userId.Value;  // assign the logged-in user
            request.Status = "Pending";          // default status

            _context.HelpRequests.Add(request);
            _context.SaveChanges();

            TempData["Success"] = "Request created successfully!";
            return RedirectToAction("Dashboard");
        }

        // GET: /Requester/Edit/5
        public IActionResult Edit(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var request = _context.HelpRequests
                .FirstOrDefault(r => r.Id == id && r.RequesterId == userId);

            if (request == null) return NotFound();
            return View(request);
        }

        // POST: /Requester/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(HelpRequest request)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null || request.RequesterId != userId)
                return RedirectToAction("Login", "User");

            if (ModelState.IsValid)
            {
                _context.HelpRequests.Update(request);
                _context.SaveChanges();
                TempData["Success"] = "Request updated successfully!";
                return RedirectToAction("Dashboard");
            }
            return View(request);
        }

        // GET: /Requester/Delete/5
        public IActionResult Delete(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var request = _context.HelpRequests
                .FirstOrDefault(r => r.Id == id && r.RequesterId == userId);

            if (request == null) return NotFound();

            _context.HelpRequests.Remove(request);
            _context.SaveChanges();
            TempData["Success"] = "Request deleted successfully!";
            return RedirectToAction("Dashboard");
        }
    }
}
