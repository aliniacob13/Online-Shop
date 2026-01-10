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
    public class CartController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;

        public CartController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            db = context;
            _userManager = userManager;
        }

        //afisare cart
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var cartItems = await db.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();
            return View(cartItems);
        }

        //adauga produs in cart (apelata prin POST cand userul este deja logat)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId, int quantity = 1)
        {
            var userId = _userManager.GetUserId(User);

            var product = await db.Products.FindAsync(productId);
            if (product == null)
            {
                TempData["message"] = "Produsul nu exista.";
                TempData["messagetype"] = "alert-danger";
                return RedirectToAction("Index", "Products");
            }

            if (product.Stock < quantity)
            {
                TempData["message"] = "Stoc insuficient pentru produsul selectat.";
                TempData["messagetype"] = "alert-danger";
                return RedirectToAction("Show", "Products", new { id = productId });
            }

            var existingItem = await db.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                db.CartItems.Add(new CartItem
                {
                    UserId = userId,
                    ProductId = productId,
                    Quantity = quantity
                });
            }

            await db.SaveChangesAsync();

            TempData["message"] = "Produs adaugat in cos!";
            TempData["messagetype"] = "alert-success";

            return RedirectToAction("Index", "Products");
        }

        //Adauga produs in cart dupa login 
        [HttpGet]
        public async Task<IActionResult> AddAfterLogin(int productId, int quantity = 1)
        {
            var userId = _userManager.GetUserId(User);

            var product = await db.Products.FindAsync(productId);
            if (product == null)
            {
                TempData["message"] = "Produsul nu exista.";
                TempData["messagetype"] = "alert-danger";
                return RedirectToAction("Index", "Products");
            }

            if (product.Stock < quantity)
            {
                TempData["message"] = "Stoc insuficient pentru produsul selectat.";
                TempData["messagetype"] = "alert-danger";
                return RedirectToAction("Index", "Products");
            }

            var existingItem = await db.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                db.CartItems.Add(new CartItem
                {
                    UserId = userId,
                    ProductId = productId,
                    Quantity = quantity
                });
            }

            await db.SaveChangesAsync();

            TempData["message"] = "Produsul a fost adaugat in cos.";
            TempData["messagetype"] = "alert-success";

            // Dupa login, vrem sa ducem utilizatorul direct in cos
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id)
        {
            var userId = _userManager.GetUserId(User);
            var cartItem = await db.CartItems
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (cartItem != null)
            {
                db.CartItems.Remove(cartItem);
                await db.SaveChangesAsync();
                TempData["message"] = "Produs eliminat din cos.";
                TempData["messagetype"] = "alert-success";
            }

            return RedirectToAction(nameof(Index));
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int id, int quantity)
        {
            var userId = _userManager.GetUserId(User);
            var cartItem = await db.CartItems
                .Include(c => c.Product)
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (cartItem == null)
            {
                TempData["message"] = "Produsul nu exista in cos.";
                TempData["messagetype"] = "alert-danger";
                return RedirectToAction(nameof(Index));
            }

            if (quantity <= 0)
            {
                db.CartItems.Remove(cartItem);
            }
            else if (quantity > cartItem.Product.Stock)
            {
                TempData["message"] = "Cantitate prea mare. Stoc insuficient.";
                TempData["messagetype"] = "alert-danger";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                cartItem.Quantity = quantity;
            }

            await db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout()
        {
            var userId = _userManager.GetUserId(User);
            var cartItems = await db.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any())
            {
                TempData["message"] = "Cosul este gol.";
                TempData["messagetype"] = "alert-danger";
                return RedirectToAction(nameof(Index));
            }

            bool ok = true;

            foreach (var item in cartItems)
            {
                if (item.Product.Stock < item.Quantity)
                {
                    TempData["message"] = $"Stoc insuficient pentru {item.Product.Title}.";
                    TempData["messagetype"] = "alert-danger";
                    ok = false;
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    item.Product.Stock -= item.Quantity;
                }
            }

            if (ok == true)
            {
                var order = new Order
                {
                    UserId = userId,
                    OrderItems = cartItems.Select(c => new OrderItem
                    {
                        ProductId = c.ProductId,
                        Quantity = c.Quantity,

                        // Snapshot la momentul cumpararii
                        ProductTitleAtPurchase = c.Product.Title,
                        UnitPriceAtPurchase = c.Product.Price,
                        ImageUrlAtPurchase = c.Product.ImageUrl
                    }).ToList()
                };

                db.Orders.Add(order);
            }

            db.CartItems.RemoveRange(cartItems);
            await db.SaveChangesAsync();

            TempData["message"] = "Comanda a fost plasata cu succes!";
            TempData["messagetype"] = "alert-success";

            return RedirectToAction("Index", "Orders");
        }
    }
}