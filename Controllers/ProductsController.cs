using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Models;
using OnlineShop.Data;
using OnlineShop.ViewModels;
using OnlineShop.Services;

namespace OnlineShop.Controllers;

public class ProductsController : Controller
{
    private readonly ApplicationDbContext db;
    private readonly IWebHostEnvironment whe;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IProductAssistantService produsAssistant;
    public ProductsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IWebHostEnvironment webHostEnvironment,
        IProductAssistantService produsAssistantService)
    {
        db = context;
        _userManager = userManager;
        _roleManager = roleManager;
        whe = webHostEnvironment;
        produsAssistant = produsAssistantService;
    }
    
    [HttpGet]
    [Authorize(Roles="Editor,Admin")]
    public IActionResult Create()
    {
        // trebuie sa trimitem lista de categorii catre View pentru a popula Dropdown-ul
        ViewBag.CategoryId = new SelectList(db.Categories, "Id", "Name");
        
        return View();
    }

    [Authorize(Roles="Editor,Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    // primim obiectul Product si fișierul separat (IFormFile imageFile)
    public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
    {
        if (imageFile == null || imageFile.Length == 0)
        {
            ModelState.AddModelError("ImageUrl", "Te rugăm să încarci o imagine.");
        }
        else
        {
            //verificam extensia(sa fie doar imagine)
            ModelState.Remove("ImageUrl");
            string extension = Path.GetExtension(imageFile.FileName).ToLower();
            string[] permittedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
            
            if (!permittedExtensions.Contains(extension))
            {
                ModelState.AddModelError("ImageUrl", "Format invalid. Doar .jpg, .jpeg, .png, .gif sunt acceptate.");
            }

            //verificam dimensiunea 
            if (imageFile.Length > 5 * 1024 * 1024) 
            {
                 ModelState.AddModelError("ImageUrl", "Imaginea este prea mare. Maxim 5MB.");
            }
        }

        //pentru ca Category este null(vine doar CategoryId din form), 
        //trebuie sa scoatem eroarea de validare automată pentru obiectul Category
        ModelState.Remove("Category"); 
        product.UserId = _userManager.GetUserId(User);



        if (User.IsInRole("Admin"))
        {
            product.Status = ProductStatus.Approved;
            product.AdminFeedback = "Aprobat automat (admin).";
        }
        else
        {
            product.Status = ProductStatus.Pending;
            product.AdminFeedback = null;
        }



        if (ModelState.IsValid)
        {
            
            //nume unic fisierului pentru a evita duplicatele
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
            
            //compunem calea unde o salvam: wwwroot/images
            string uploadsFolder = Path.Combine(whe.WebRootPath, "images");
            
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            //copiem fisierul primit in calea definita
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }

            //salvare in baza de date
            
            //doar calea relativa în obiectul produs
            product.ImageUrl = "/images/" + uniqueFileName;

            db.Add(product);
            TempData["message"] = "Produsul a fost adaugat cu succes!";
            await db.SaveChangesAsync();
            return RedirectToAction(nameof(Index)); 
        }

        ViewBag.CategoryId = new SelectList(db.Categories, "Id", "Name", product.CategoryId);
        return View(product);
    }

    /*
    public async Task<IActionResult> Index()
    {
        var produse = await db.Products
            .Where(a => a.Status == ProductStatus.Approved)
            .Include(a=>a.Category)
            .ToListAsync(); 

        return View(produse);
    }
   */
    //am pus parametru nullable categoryId pentru ca
    //utilizatorul sa poata filtra produsele in functie de categorie
    [AllowAnonymous]
public async Task<IActionResult> Index(List<int>? categoriiSelectate, string? searchString, string? sortarePret, int page = 1)
{
    int perPage = 6;
    if (page < 1) page = 1;

    //lista de categorii pentru sidebar
    var categorii = await db.Categories
        .OrderBy(c => c.Name)
        .ToListAsync();
    ViewBag.Categorii = categorii;

    categoriiSelectate ??= new List<int>();
    ViewBag.CategoriiSelectate = categoriiSelectate;
    ViewBag.SearchString = searchString;
    ViewBag.SortarePret = sortarePret;

    var produseQuery = db.Products
        .Where(p => p.Status == ProductStatus.Approved)
        .Include(p => p.Category)
        .Include(p => p.Reviews)
        .AsQueryable();

    //multi-categorii
    if (categoriiSelectate.Any())
    {
        produseQuery = produseQuery.Where(p => categoriiSelectate.Contains(p.CategoryId));
        ViewBag.CategoryName = "Categorii selectate";
    }
    else
    {
        ViewBag.CategoryName = "Toate produsele";
    }

    //cautare
    if (!string.IsNullOrWhiteSpace(searchString))
    {
        produseQuery = produseQuery.Where(p => EF.Functions.Like(p.Title, $"%{searchString}%"));
    }

    //sortare pret
    //sortare(pret / rating)
    //sortare: intai cele in stoc, apoi cele fara stoc (Stock == 0)
    if (sortarePret == "pret_crescator")
    {
        produseQuery = produseQuery
            .OrderBy(p => p.Stock == 0)
            .ThenBy(p => p.Price)
            .ThenBy(p => p.Id);
    }
    else if (sortarePret == "pret_descrescator")
    {
        produseQuery = produseQuery
            .OrderBy(p => p.Stock == 0)
            .ThenByDescending(p => p.Price)
            .ThenBy(p => p.Id);
    }
    else if (sortarePret == "rating_crescator")
    {
        produseQuery = produseQuery
            .OrderBy(p => p.Stock == 0)
            .ThenBy(p => p.Reviews.Where(r => r.Rating.HasValue)
                .Select(r => (double?)r.Rating!.Value)
                .Average() ?? 0)
            .ThenBy(p => p.Id);
    }
    else if (sortarePret == "rating_descrescator")
    {
        produseQuery = produseQuery
            .OrderBy(p => p.Stock == 0)
            .ThenByDescending(p => p.Reviews.Where(r => r.Rating.HasValue)
                .Select(r => (double?)r.Rating!.Value)
                .Average() ?? 0)
            .ThenBy(p => p.Id);
    }
    else
    {
        produseQuery = produseQuery
            .OrderBy(p => p.Stock == 0)
            .ThenBy(p => p.Id);
    }

    //paginare
    int totalItems = await produseQuery.CountAsync();
    int lastPage = (int)Math.Ceiling(totalItems / (double)perPage);
    if (lastPage < 1) lastPage = 1;
    if (page > lastPage) page = lastPage;

    int offset = (page - 1) * perPage;

    var produsePagina = await produseQuery
        .Skip(offset)
        .Take(perPage)
        .ToListAsync();

    ViewBag.lastPage = lastPage;
    ViewBag.CurrentPage = page;

    //base url pentru paginare care pastreaza filtrele
    var baseUrl = Url.Action("Index", "Products", new
    {
        categoriiSelectate = categoriiSelectate,
        searchString = searchString,
        sortarePret = sortarePret
    }) ?? "/Products/Index";

    var separator = baseUrl.Contains("?") ? "&" : "?";
    ViewBag.PaginationBaseUrl = baseUrl + separator + "page=";

    return View(produsePagina);
}

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AskAssistant(int productId, string message)
    {
        var userId = (User.Identity != null && User.Identity.IsAuthenticated)
            ? _userManager.GetUserId(User)
            : null;

        var rezultat = await produsAssistant.AskAsync(productId, message, userId);

        return Json(new
        {
            success = rezultat.Success,
            answer = rezultat.Answer
        });
    }

    //se "sterge" un produs din shop 
    [HttpPost]
    [Authorize(Roles="Editor,Admin")] 
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await db.Products
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
        {
            return NotFound();
        }

        var currentUserId = _userManager.GetUserId(User);

        //admin poate sterge orice produs
        //editor poate sterge doar produsele proprii
        if (User.IsInRole("Admin") || product.UserId == currentUserId)
        {
            //daca e deja sters, nu mai facem nimic
            if (!product.IsDeleted)
            {
                product.IsDeleted = true;
                product.DeletedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }

            TempData["message"] = "Produsul a fost scos din shop.";
            TempData["message type"] = "alert-success";
            return RedirectToAction("Index");
        }
        else
        {
            TempData["message"] = "Nu aveti dreptul sa stergeti acest produs.";
            TempData["message type"] = "alert-danger";
            return RedirectToAction("Index");
        }
    }

    public IActionResult IndexNou()
    {
        return View();
    }

    public IEnumerable<SelectListItem> GetAllCategories()
    {
        //generam o lista de tipul SelectListItem fara elemente
        var selecList=new List<SelectListItem>();
        //extragem toate categoriile din baza de date
        var categories=db.Categories.ToList();
        //pentru fiecare categorie adaugam un element in lista
        foreach (var category in categories)
        {
            selecList.Add(new SelectListItem{Value=category.Id.ToString(),Text=category.Name});
        }
        return selecList;
    }
    
    
    [HttpGet]
    [Authorize(Roles = "Editor,Admin")]
    public IActionResult Edit(int id)
{
    var produs = db.Products
        .Include(p => p.Category)
        .FirstOrDefault(p => p.Id == id);

    if (produs == null)
    {
        return NotFound();
    }

    var vm = new ProductEditViewModel
    {
        Id = produs.Id,
        Title = produs.Title,
        Description = produs.Description,
        Price = produs.Price,
        Stock = produs.Stock,
        CategoryId = produs.CategoryId,
        ImageUrlCurenta = produs.ImageUrl
    };

    ViewBag.CategoryId = new SelectList(db.Categories, "Id", "Name", vm.CategoryId);
    if(produs.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
    {return View(vm);}
    else
    {
        TempData["message"] = "Nu aveti dreptul sa faceti modificari asupra unui produs care nu va apartine";
        TempData["messagetype"] = "alert-danger";
        return RedirectToAction("Index");
    }
}
[Authorize]
[HttpPost]
public async Task<IActionResult> Edit(ProductEditViewModel model)
{
    var produs = db.Products.FirstOrDefault(p => p.Id == model.Id);
    if (produs == null)
    {
        return NotFound();
    }

    //verificare drepturi: proprietar sau admin
    if (produs.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
    {
        if (model.CategoryId == null)
        {
            ModelState.AddModelError("CategoryId", "Selectati o categorie");
        }
        
        if (model.Price.HasValue && model.Price.Value <= 0)
        {
            ModelState.AddModelError(nameof(model.Price), "Prețul trebuie să fie mai mare decât 0.");
        }
        
        if (!ModelState.IsValid)
        {
            ViewBag.CategoryId = new SelectList(db.Categories, "Id", "Name", model.CategoryId);
            return View(model);
        }

        if (!string.IsNullOrWhiteSpace(model.Title))
            produs.Title = model.Title;

        if (!string.IsNullOrWhiteSpace(model.Description))
            produs.Description = model.Description;

        if (model.Price.HasValue)
            produs.Price = model.Price.Value;

        if (model.Stock.HasValue)
            produs.Stock = model.Stock.Value;

        if (model.CategoryId.HasValue)
            produs.CategoryId = model.CategoryId.Value;

        //imagine noua optionala
        if (model.ImagineNoua != null && model.ImagineNoua.Length > 0)
        {
            string extensie = Path.GetExtension(model.ImagineNoua.FileName).ToLower();
            string[] permise = { ".jpg", ".jpeg", ".png", ".gif" };

            if (!permise.Contains(extensie))
            {
                ModelState.AddModelError("ImagineNoua", "Format invalid pentru imagine.");
                ViewBag.CategoryId = new SelectList(db.Categories, "Id", "Name", model.CategoryId);
                return View(model);
            }

            string numeUnic = Guid.NewGuid() + "_" + model.ImagineNoua.FileName;
            string folder = Path.Combine(whe.WebRootPath, "images");

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            string caleFisier = Path.Combine(folder, numeUnic);

            using var fisierStream = new FileStream(caleFisier, FileMode.Create);
            await model.ImagineNoua.CopyToAsync(fisierStream);

            produs.ImageUrl = "/images/" + numeUnic;
        }




            if (User.IsInRole("Admin"))
            {
                produs.Status = ProductStatus.Approved;//daca adminul editeaza un produs, e deja aprobat
                produs.AdminFeedback = null;
            }
            else
            {
                produs.Status = ProductStatus.Pending;//daca un editor editeaza un produs, revine in starea de pending
                produs.AdminFeedback = null;
            }




            await db.SaveChangesAsync();

        TempData["message"] = "Produsul a fost modificat";
        TempData["messagetype"] = "alert-success";

        return RedirectToAction("Index");
    }
    else
    {
        TempData["message"] = "Nu aveti dreptul sa faceti modificari asupra unui produs care nu va apartine";
        TempData["messagetype"] = "alert-danger";
        return RedirectToAction("Index");
    }
}

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Show(int id)
    {
    var produs = await db.Products
        .Include(a => a.Category)
        .Include(a => a.Reviews)
        .ThenInclude(r => r.User)
        .FirstOrDefaultAsync(a => a.Id == id);

    if (produs is null)
    {
        return NotFound();
    }

    //media rating pentru produs (doar reviewuri cu Rating setat)
    double? avgRating = null;
    if (produs.Reviews != null && produs.Reviews.Any(r => r.Rating.HasValue))
    {
        avgRating = produs.Reviews
            .Where(r => r.Rating.HasValue)
            .Average(r => r.Rating!.Value);
    }
    ViewBag.AverageRating = avgRating;

    //daca poate userul curent sa lase review
    bool canReview = false;
    if (User.Identity != null && User.Identity.IsAuthenticated)
    {
        var userId = _userManager.GetUserId(User);

        canReview = await db.Orders
            .Include(o => o.OrderItems)
            .AnyAsync(o => o.UserId == userId &&
                           o.OrderItems.Any(oi => oi.ProductId == produs.Id));
    }
    ViewBag.CanReview = canReview;

    return View(produs);
    }






    //pt fluxul admin - aprobare produse


    [Authorize(Roles="Admin")]
    public async Task<IActionResult> Approve(int id)
    {
        var product = await db.Products.FindAsync(id);
        product.Status = ProductStatus.Approved;
        product.AdminFeedback = "Produsul a fost aprobat!";
        await db.SaveChangesAsync();

        return RedirectToAction("PendingProducts");
    }

    [Authorize(Roles="Admin")]
    public async Task<IActionResult> Reject(int id, string feedback)
    {
        var product = await db.Products.FindAsync(id);
        product.Status = ProductStatus.Rejected;

        product.AdminFeedback = string.IsNullOrWhiteSpace(feedback) ? "Produs respins de admin. Fara feedback.": feedback;

        await db.SaveChangesAsync();

        return RedirectToAction("PendingProducts");
    }

    //adminul vede lista produselor in asteptare
    [Authorize(Roles="Admin")]
    public async Task<IActionResult> PendingProducts()
    {
        var products = await db.Products
            .Include(p => p.Category)
            .Where(p => p.Status == ProductStatus.Pending)
            .Include(p => p.User)
            .ToListAsync();

        return View(products);
    }




    //ce vede colaboratorul
    [Authorize(Roles = "Editor")]
    public async Task<IActionResult> ProposedProducts()
    {
        var userId = _userManager.GetUserId(User);

        var produse = await db.Products
            .Include(p => p.Category)
            .Where(p => p.UserId == userId)          
            .OrderByDescending(p => p.Id)
            .ToListAsync();

        return View(produse);
    }




}