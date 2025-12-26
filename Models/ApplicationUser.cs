using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.General;

namespace OnlineShop.Models;

public class ApplicationUser : IdentityUser
{
    public ICollection<CartItem>? CartItems { get; set; } = new List<CartItem>();
    public ICollection<WishlistItem>? WishlistItems { get; set; } = new List<WishlistItem>();
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    //variabila in care vom retine rolurile existente in baza de date pentru popularea 
    //unui dropdown list 
    [NotMapped]
    public IEnumerable<SelectListItem>? AllRoles { get; set; }
}