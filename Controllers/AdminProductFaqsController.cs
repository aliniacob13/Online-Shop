using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Data;
using OnlineShop.Models;

namespace OnlineShop.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminProductFaqsController : Controller
    {
        private readonly ApplicationDbContext db;

        public AdminProductFaqsController(ApplicationDbContext context)
        {
            db = context;
        }

        // /AdminProductFaqs?productId=5
        public async Task<IActionResult> Index(int? productId)
        {
            ViewBag.ProductId = productId;

            var query = db.ProductFaqs
                .Include(f => f.Product)
                .AsNoTracking()
                .AsQueryable();

            if (productId.HasValue)
                query = query.Where(f => f.ProductId == productId.Value);

            var lista = await query
                .OrderByDescending(f => f.IsApproved)
                .ThenByDescending(f => f.Id)
                .ToListAsync();

            return View(lista);
        }

        [HttpGet]
        public IActionResult Create(int productId, string? question = null)
        {
            ViewBag.ProductId = productId;
            var faq = new ProductFaq
            {
                ProductId = productId,
                Question = question ?? "",
                Answer = "",
                IsApproved = false
            };
            return View(faq);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductFaq faq)
        {
            ModelState.Remove("Product");

            if (!ModelState.IsValid)
            {
                ViewBag.ProductId = faq.ProductId;
                return View(faq);
            }

            db.ProductFaqs.Add(faq);
            await db.SaveChangesAsync();
            TempData["message"] = "FAQ adaugat.";
            return RedirectToAction(nameof(Index), new { productId = faq.ProductId });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var faq = await db.ProductFaqs.FindAsync(id);
            if (faq == null) return NotFound();
            return View(faq);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductFaq cerut)
        {
            ModelState.Remove("Product");

            var faq = await db.ProductFaqs.FindAsync(id);
            if (faq == null) return NotFound();

            if (!ModelState.IsValid) return View(cerut);

            faq.Question = cerut.Question;
            faq.Answer = cerut.Answer;
            faq.IsApproved = cerut.IsApproved;

            await db.SaveChangesAsync();
            TempData["message"] = "FAQ modificat.";
            return RedirectToAction(nameof(Index), new { productId = faq.ProductId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleApprove(int id)
        {
            var faq = await db.ProductFaqs.FindAsync(id);
            if (faq == null) return NotFound();

            faq.IsApproved = !faq.IsApproved;
            await db.SaveChangesAsync();

            TempData["message"] = faq.IsApproved ? "FAQ aprobat." : "FAQ dezaprobat.";
            return RedirectToAction(nameof(Index), new { productId = faq.ProductId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var faq = await db.ProductFaqs.FindAsync(id);
            if (faq == null) return NotFound();

            var pid = faq.ProductId;
            db.ProductFaqs.Remove(faq);
            await db.SaveChangesAsync();

            TempData["message"] = "FAQ sters.";
            return RedirectToAction(nameof(Index), new { productId = pid });
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var faq = await db.ProductFaqs.FindAsync(id);
            if (faq == null) return NotFound();

            faq.IsApproved = true;
            await db.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { productId = faq.ProductId });
        }
    }
}