using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Mvc;
using OnlineShop.Models;
using OnlineShop.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace OnlineShop.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager
        )
        {
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index(string? email, List<string>? roluriSelectate, int page = 1)
        {
            int perPage = 6;
            if (page < 1) page = 1;

            roluriSelectate ??= new List<string>();

            var roluriDisponibile = new List<string> { "Admin", "Editor", "User" };
            ViewBag.RoluriDisponibile = roluriDisponibile;
            ViewBag.RoluriSelectate = roluriSelectate;

            ViewBag.EmailSearch = email ?? "";

            var usersQuery = FiltreazaUseriDupaEmail(email).AsQueryable();

            if (roluriSelectate.Any())
            {
                var userIdsCuRol = db.UserRoles
                    .Join(db.Roles,
                        ur => ur.RoleId,
                        r => r.Id,
                        (ur, r) => new { ur.UserId, Rol = r.Name })
                    .Where(x => x.Rol != null && roluriSelectate.Contains(x.Rol))
                    .Select(x => x.UserId)
                    .Distinct();

                usersQuery = db.Users.Where(u => userIdsCuRol.Contains(u.Id));
            }

            usersQuery = usersQuery.OrderBy(u => u.UserName);

            // Paginare
            int totalItems = await usersQuery.CountAsync();
            int lastPage = (int)Math.Ceiling(totalItems / (double)perPage);
            if (lastPage < 1) lastPage = 1;
            if (page > lastPage) page = lastPage;

            int offset = (page - 1) * perPage;

            var usersPagina = await usersQuery
                .Skip(offset)
                .Take(perPage)
                .ToListAsync();

            ViewBag.UsersList = usersPagina;
            ViewBag.lastPage = lastPage;
            ViewBag.CurrentPage = page;

            // Base url pentru paginare care pastreaza filtrele (ca la produse)
            var baseUrl = Url.Action("Index", "Users", new
            {
                email = email,
                roluriSelectate = roluriSelectate
            }) ?? "/Users/Index";

            var separator = baseUrl.Contains("?") ? "&" : "?";
            ViewBag.PaginationBaseUrl = baseUrl + separator + "page=";

            return View();
        }

        public async Task<ActionResult> ShowAsync(string id)
        {
            ApplicationUser? user = db.Users.Find(id);
            if (user is null)
            {
                return NotFound();
            }
            else
            {
                var roles=await _userManager.GetRolesAsync(user);
                ViewBag.Roles = roles;
                ViewBag.UserCurent = await _userManager.GetUserAsync(User);
                return View(user);
            }
        }

        public async Task<IActionResult> Edit(string id)
        {
            ApplicationUser? user = db.Users.Find(id);
            if (user is null)
            {
                return NotFound();
            }
            else
            {
                ViewBag.AllRoles = GetAllRoles();
                var roleNames=await _userManager.GetRolesAsync(user); //obtinem numele rolurilor utilizatorului
                ViewBag.UserRole = _roleManager.Roles
                    .Where(r => roleNames.Contains(r.Name))
                    .Select(r => r.Id)
                    .First(); 
                return View(user);
            }
        }

        [HttpPost]
        public async Task<ActionResult> Edit(string id, ApplicationUser newData, [FromForm] string newRole)
        {
            ApplicationUser? user = db.Users.Find(id);
            if (user is null)
            {
                return NotFound();
            }
            else
            {
                if (ModelState.IsValid)
                {
                    user.UserName = newData.UserName;
                    user.Email = newData.Email;
                    user.FirstName = newData.FirstName;
                    user.LastName = newData.LastName;
                    //cautam toate rolurile din baza de date
                    var roles=db.Roles.ToList();
                    foreach (var role in roles)
                    {
                        //scoatem userul din rolurile anterioare
                        await _userManager.RemoveFromRoleAsync(user, role.Name);
                    }
                    //Adaugam noul rol selectat
                    var roleName = await _roleManager.FindByIdAsync(newRole);
                    await _userManager.AddToRoleAsync(user, roleName.ToString());
                    db.SaveChanges();
                }

                user.AllRoles = GetAllRoles();
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public IActionResult Delete(string id)
        {
            var user = db.Users
                .Include(u => u.CartItems)
                .Include(u => u.WishlistItems)
                .Where(u => u.Id == id)
                .First();
            if (user == null)
            {
                return NotFound();
            }
            //delete user cart items
            if (user.CartItems != null && user.CartItems.Any())
            {
                db.CartItems.RemoveRange(user.CartItems);
            }
            // delete user wishlist items
            if (user.WishlistItems != null && user.WishlistItems.Any())
            {
                db.WishlistItems.RemoveRange(user.WishlistItems);
            }
            // delete user
            db.Users.Remove(user);

            db.SaveChanges();

            return RedirectToAction("Index");
        }
        
        [NonAction]
        public IQueryable<ApplicationUser> FiltreazaUseriDupaEmail(string? emailSauPrefix)
        {
            var query = db.Users.AsQueryable();

            if (string.IsNullOrWhiteSpace(emailSauPrefix))
                return query;

            var term = emailSauPrefix.Trim();

            // Cautare dupa email sau prefix din email
            return query.Where(u => u.Email != null && u.Email.StartsWith(term));
        }
        [NonAction]
        public IEnumerable<SelectListItem> GetAllRoles()
        {
            var selectList=new List<SelectListItem>();
            var roles= from role in db.Roles select role;
            foreach (var role in roles)
            {
                selectList.Add(new SelectListItem{Value=role.Id,Text=role.Name});
            }
            return selectList;
        }
    }
}