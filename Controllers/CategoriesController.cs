using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Data;
using OnlineShop.Models;

namespace OnlineShop.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;

        public CategoriesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            db = context;
            _userManager = userManager;
        }

        // GET /Categories
        public ActionResult Index()
        {
            if (TempData.ContainsKey("message"))
            {
                ViewBag.message = TempData["message"];
            }

            var categories = db.Categories
                .OrderBy(c => c.Name)
                .ToList();

            return View(categories);
        }

        // GET /Categories/New  - doar Admin sau Editor pot vedea formularul
        [Authorize(Roles = "Admin,Editor")]
        public ActionResult New()
        {
            return View("Create");
        }

        // POST /Categories/New - doar Admin sau Editor pot crea
        [HttpPost]
        [Authorize(Roles = "Admin,Editor")]
        [ValidateAntiForgeryToken]
        public ActionResult New(Category cat)
        {
            if (ModelState.IsValid)
            {
                // Creatorul devine proprietar
                cat.UserId = _userManager.GetUserId(User);

                db.Categories.Add(cat);
                db.SaveChanges();
                TempData["message"] = "Categoria a fost adaugata";
                return RedirectToAction("Index");
            }

            return View("Create", cat);
        }

        // GET /Categories/Edit/{id}
        [Authorize] // trebuie sa fii logat macar
        public ActionResult Edit(int id)
        {
            var category = db.Categories.Find(id);
            if (category is null)
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);

            if (User.IsInRole("Admin") ||
                User.IsInRole("Editor") ||
                category.UserId == currentUserId)
            {
                return View(category);
            }

            TempData["message"] = "Nu aveti dreptul sa editati aceasta categorie.";
            return RedirectToAction("Index");
        }

        // POST /Categories/Edit/{id}
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, Category requestedCategory)
        {
            var category = db.Categories.Find(id);
            if (category is null)
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);

            if (!(User.IsInRole("Admin") ||
                  User.IsInRole("Editor") ||
                  category.UserId == currentUserId))
            {
                TempData["message"] = "Nu aveti dreptul sa editati aceasta categorie.";
                return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                category.Name = requestedCategory.Name;
                db.SaveChanges();
                TempData["message"] = "Categoria a fost modificata!";
                return RedirectToAction("Index");
            }

            return View(requestedCategory);
        }

        // POST /Categories/Delete/{id}
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            // folosim IgnoreQueryFilters ca sa gasim categoria chiar daca e deja soft-deleted
            var category = db.Categories
                .IgnoreQueryFilters()
                .Include(c => c.Products)
                .FirstOrDefault(c => c.Id == id);

            if (category == null)
            {
                TempData["message"] = $"Categoria cu id {id} nu a fost gasita.";
                return RedirectToAction("Index");
            }

            var currentUserId = _userManager.GetUserId(User);

            if (!(User.IsInRole("Admin") ||
                  User.IsInRole("Editor") ||
                  category.UserId == currentUserId))
            {
                TempData["message"] = "Nu aveti dreptul sa stergeti aceasta categorie.";
                return RedirectToAction("Index");
            }

            try
            {
                var now = DateTime.UtcNow;

                // 1) soft delete categorie
                if (!category.IsDeleted)
                {
                    category.IsDeleted = true;
                    category.DeletedAt = now;
                }

                // 2) soft delete produse din categorie
                foreach (var p in category.Products)
                {
                    if (!p.IsDeleted)
                    {
                        p.IsDeleted = true;
                        p.DeletedAt = now;
                    }
                }

                // 3) recomandat: sterge din cos/wishlist produsele scoase din shop
                var productIds = category.Products.Select(p => p.Id).ToList();

                var cartToRemove = db.CartItems.Where(ci => productIds.Contains(ci.ProductId)).ToList();
                db.CartItems.RemoveRange(cartToRemove);

                var wishToRemove = db.WishlistItems.Where(wi => productIds.Contains(wi.ProductId)).ToList();
                db.WishlistItems.RemoveRange(wishToRemove);

                db.SaveChanges();
                TempData["message"] = "Categoria si produsele ei au fost scoase din shop.";
            }
            catch (Exception ex)
            {
                TempData["message"] = "Categoria nu a putut fi stearsa. Motiv: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // Optional: Show ramane public
        public ActionResult Show(int id)
        {
            var category = db.Categories.Find(id);
            if (category is null)
            {
                return NotFound();
            }

            return View(category);
        }
    }
}