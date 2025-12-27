using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Data;

namespace OnlineShop.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminTopQuestionsController : Controller
    {
        private readonly ApplicationDbContext db;

        public AdminTopQuestionsController(ApplicationDbContext context)
        {
            db = context;
        }

        // /AdminTopQuestions?days=30
        public async Task<IActionResult> Index(int days = 30)
        {
            var deLa = DateTime.Now.AddDays(-days);

            var logs = await db.ProductQuestionLogs
                .Include(l => l.Product)
                .AsNoTracking()
                .Where(l => l.AskedAt >= deLa)
                .ToListAsync();

            string Normalizare(string s)
            {
                s = (s ?? "").Trim().ToLowerInvariant();
                s = new string(s.Where(ch => char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch)).ToArray());
                s = string.Join(' ', s.Split(' ', StringSplitOptions.RemoveEmptyEntries));
                return s;
            }

            var top = logs
                .GroupBy(l => new { l.ProductId, Q = Normalizare(l.Question) })
                .Select(g => new TopQVm
                {
                    ProductId = g.Key.ProductId,
                    ProductTitle = g.First().Product?.Title ?? "",
                    Question = g.First().Question,
                    Count = g.Count(),
                    LastAsked = g.Max(x => x.AskedAt)
                })
                .OrderByDescending(x => x.Count)
                .ThenByDescending(x => x.LastAsked)
                .Take(200)
                .ToList();

            ViewBag.Days = days;
            return View(top);
        }

        public class TopQVm
        {
            public int ProductId { get; set; }
            public string ProductTitle { get; set; } = "";
            public string Question { get; set; } = "";
            public int Count { get; set; }
            public DateTime LastAsked { get; set; }
        }
    }
}