namespace OnlineShop.Models;
using System.ComponentModel.DataAnnotations;
public class Category
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Numele categoriei este obligatoriu!")]
    [StringLength(50, MinimumLength = 5, ErrorMessage = "Numele categoriei trebuie să aibă între 5 și 50 de caractere.")]
    public string Name { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
}