using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Data;
using OnlineShop.ViewModels.AdminTopQuestions;

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

        // /AdminTopQuestions?days=30&page=1
        public async Task<IActionResult> Index(int days = 30, int page = 1)
        {
            int perPage = 10;
            if (page < 1) page = 1;
            if (days < 1) days = 1;
            if (days > 365) days = 365;

            var deLa = DateTime.Now.AddDays(-days);

            var logs = await db.ProductQuestionLogs
                .Include(l => l.Product)
                .AsNoTracking()
                .Where(l => l.AskedAt >= deLa)
                .ToListAsync();

            static string Normalizare(string s)
            {
                s = (s ?? "").Trim().ToLowerInvariant();
                s = new string(s.Where(ch => char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch)).ToArray());
                s = string.Join(' ', s.Split(' ', StringSplitOptions.RemoveEmptyEntries));
                return s;
            }

            var topAll = logs
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

            int totalItems = topAll.Count;
            int lastPage = (int)Math.Ceiling(totalItems / (double)perPage);
            if (lastPage < 1) lastPage = 1;
            if (page > lastPage) page = lastPage;

            var topPagina = topAll
                .Skip((page - 1) * perPage)
                .Take(perPage)
                .ToList();

            ViewBag.Days = days;
            ViewBag.lastPage = lastPage;
            ViewBag.CurrentPage = page;

            var baseUrl = Url.Action("Index", "AdminTopQuestions", new { days = days }) ?? "/AdminTopQuestions/Index";
            var separator = baseUrl.Contains("?") ? "&" : "?";
            ViewBag.PaginationBaseUrl = baseUrl + separator + "page=";

            return View(topPagina);
        }
    }
}