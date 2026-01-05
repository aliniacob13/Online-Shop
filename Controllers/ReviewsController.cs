using OnlineShop.Models;
using OnlineShop.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace OnlineShop.Controllers
{
    [Authorize(Roles = "Admin,Editor,User")]
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReviewsController(ApplicationDbContext context,
                                 UserManager<ApplicationUser> userManager)
        {
            db = context;
            _userManager = userManager;
        }

        // Creeaza un review nou
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Review review)
        {
            // Product si User sunt navigation properties, nu vin din formular
            ModelState.Remove("Product");
            ModelState.Remove("User");

            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Challenge();
            }

            // Verificam daca userul a cumparat produsul
            bool hasPurchased = await db.Orders
                .Include(o => o.OrderItems)
                .AnyAsync(o => o.UserId == userId &&
                               o.OrderItems.Any(oi => oi.ProductId == review.ProductId));

            if (!hasPurchased)
            {
                TempData["message"] = "Poti lasa review doar pentru produse pe care le-ai achizitionat.";
                TempData["messagetype"] = "alert-danger";
                return RedirectToAction("Show", "Products", new { id = review.ProductId });
            }

            review.UserId = userId;
            review.DatePosted = DateTime.Now;

            if (!ModelState.IsValid)
            {
                var product = await db.Products
                    .Include(p => p.Reviews)
                    .ThenInclude(r => r.User)
                    .FirstOrDefaultAsync(p => p.Id == review.ProductId);

                if (product == null)
                {
                    return NotFound();
                }

                // medie rating pentru view (luam doar reviewuri cu Rating setat)
                double? avg = null;
                if (product.Reviews.Any(r => r.Rating.HasValue))
                {
                    avg = product.Reviews
                        .Where(r => r.Rating.HasValue)
                        .Average(r => r.Rating!.Value);
                }
                ViewBag.AverageRating = avg;

                ViewBag.CanReview = hasPurchased;

                return View("~/Views/Products/Show.cshtml", product);
            }

            db.Reviews.Add(review);
            await db.SaveChangesAsync();

            return RedirectToAction("Show", "Products", new { id = review.ProductId });
        }

        // Stergere review
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var review = await db.Reviews.FindAsync(id);
            if (review == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");

            // admin poate sterge orice, altfel doar autorul
            if (!isAdmin && review.UserId != userId)
            {
                TempData["message"] = "Nu poti sterge un review care nu iti apartine.";
                TempData["messagetype"] = "alert-danger";
                return RedirectToAction("Show", "Products", new { id = review.ProductId });
            }

            var productId = review.ProductId;

            db.Reviews.Remove(review);
            await db.SaveChangesAsync();

            return RedirectToAction("Show", "Products", new { id = productId });
        }

        // Editare review 
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var review = await db.Reviews.FindAsync(id);
            if (review == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin && review.UserId != userId)
            {
                TempData["message"] = "Nu poti edita un review care nu iti apartine.";
                TempData["messagetype"] = "alert-danger";
                return RedirectToAction("Show", "Products", new { id = review.ProductId });
            }

            return View(review);
        }

        // Editare review 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Review requestedReview)
        {
            var review = await db.Reviews.FindAsync(id);
            if (review == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin && review.UserId != userId)
            {
                TempData["message"] = "Nu poti edita un review care nu iti apartine.";
                TempData["messagetype"] = "alert-danger";
                return RedirectToAction("Show", "Products", new { id = review.ProductId });
            }

            if (!ModelState.IsValid)
            {
                return View(requestedReview);
            }

            review.Comment = requestedReview.Comment;
            review.Rating = requestedReview.Rating;
            review.DatePosted = DateTime.Now;

            await db.SaveChangesAsync();

            return RedirectToAction("Show", "Products", new { id = review.ProductId });
        }
    }
}