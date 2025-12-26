using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Data;
using OnlineShop.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;

namespace OnlineShop.Controllers
{
    [Authorize]
    public class WishlistController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;

        public WishlistController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            db = context;
            _userManager = userManager;
        }

        // --- Afisare wishlist ---
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var items = await db.WishlistItems
                .Include(w => w.Product)
                .Where(w => w.UserId == userId)
                .ToListAsync();
            return View(items);
        }

        // --- Adauga in wishlist (POST, cand userul e deja logat) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId)
        {
            var userId = _userManager.GetUserId(User);

            var exists = await db.WishlistItems
                .AnyAsync(w => w.UserId == userId && w.ProductId == productId);

            if (!exists)
            {
                db.WishlistItems.Add(new WishlistItem
                {
                    UserId = userId,
                    ProductId = productId
                });
                await db.SaveChangesAsync();

                TempData["message"] = "Produs adaugat in wishlist!";
                TempData["messagetype"] = "alert-success";
            }

            return RedirectToAction("Index", "Products");
        }

        // --- Adauga in wishlist dupa login (GET, folosit cu returnUrl) ---
        [HttpGet]
        public async Task<IActionResult> AddAfterLogin(int productId)
        {
            var userId = _userManager.GetUserId(User);

            var exists = await db.WishlistItems
                .AnyAsync(w => w.UserId == userId && w.ProductId == productId);

            if (!exists)
            {
                db.WishlistItems.Add(new WishlistItem
                {
                    UserId = userId,
                    ProductId = productId
                });

                await db.SaveChangesAsync();

                TempData["message"] = "Produsul a fost adaugat in wishlist.";
                TempData["messagetype"] = "alert-success";
            }

            // Dupa login, ducem utilizatorul direct la wishlist-ul lui
            return RedirectToAction(nameof(Index));
        }

        // --- Sterge din wishlist ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id)
        {
            var userId = _userManager.GetUserId(User);
            var item = await db.WishlistItems
                .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

            if (item != null)
            {
                db.WishlistItems.Remove(item);
                await db.SaveChangesAsync();

                TempData["message"] = "Produs eliminat din wishlist.";
                TempData["messagetype"] = "alert-success";
            }

            return RedirectToAction(nameof(Index));
        }

        // --- Mutare in cart ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveToCart(int id)
        {
            var userId = _userManager.GetUserId(User);

            var item = await db.WishlistItems
                .Include(w => w.Product)
                .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

            if (item == null)
            {
                TempData["message"] = "Produsul nu exista in wishlist.";
                TempData["messagetype"] = "alert-danger";
                return RedirectToAction(nameof(Index));
            }

            var cartItem = await db.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == item.ProductId);

            if (cartItem != null)
            {
                cartItem.Quantity += 1;
            }
            else
            {
                db.CartItems.Add(new CartItem
                {
                    UserId = userId,
                    ProductId = item.ProductId,
                    Quantity = 1
                });
            }

            db.WishlistItems.Remove(item);
            await db.SaveChangesAsync();

            TempData["message"] = "Produs mutat in cos!";
            TempData["messagetype"] = "alert-success";

            return RedirectToAction("Index", "Cart");
        }
    }
}