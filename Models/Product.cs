using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace OnlineShop.Models;

public enum ProductStatus
{
    Pending,   
    Approved,  
    Rejected   
}

public class Product
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Titlul produsului este obligatoriu.")]
    [StringLength(100, MinimumLength = 5, ErrorMessage = "Titlul trebuie să aibă între 5 și 100 de caractere.")]
    public string Title { get; set; }

    [Required(ErrorMessage = "Descrierea este obligatorie.")]
    public string Description { get; set; }

    //stocam calea imaginii 
    [Required(ErrorMessage = "Imaginea este obligatorie.")]
    public string ImageUrl { get; set; }

    //pret>0, folosim decimal pt bani 
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Prețul trebuie să fie mai mare decât 0.")]
    [Column(TypeName = "decimal(18,2)")] //configurarea pentru baza de date SQL
    public decimal Price { get; set; }

    //stoc >= 0
    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "Stocul nu poate fi negativ.")]
    public int Stock { get; set; }

    //flag pentru aprobare (implicit false)
    //public bool IsApproved { get; set; } = false;


    public ProductStatus Status { get; set; } = ProductStatus.Pending;
    public string? AdminFeedback { get; set; }

    
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public ICollection<ProductFaq> Faqs { get; set; } = new List<ProductFaq>();

    //RELATII

    //fk catre Categorie
    [Required(ErrorMessage = "Selectarea unei categorii este obligatorie.")]
    public int CategoryId { get; set; }
    public Category Category { get; set; }
    
    public string? UserId { get; set; }
    public virtual ApplicationUser? User { get; set; } //un articol apartine unui singur user
    //lista de reviewuri
    public ICollection<Review> Reviews { get; set; } = new List<Review>();

    //calculul pt ratingul mediu al produsului

    //aceasta proprietate nu se salveaza in baza de date deci [NotMapped],
    // ci se calculeaza automat la afisarea produsului
    [NotMapped]
    public double AverageRating
    {
        get
        {
            if (Reviews == null || Reviews.Count == 0)
            {
                return 0; //rating initial 0
            }
            
            //calculam media doar pentru review-urile care au rating (cele cu null sunt ignorate)
            var ratings = Reviews.Where(r => r.Rating.HasValue).Select(r => r.Rating.Value);
            
            if (!ratings.Any()) return 0;

            return Math.Round(ratings.Average(), 1); //se returneaza media cu o zecimala 
        }
    }
    
    [NotMapped]
    public IEnumerable<SelectListItem>? Categ { get; set; }=Enumerable.Empty<SelectListItem>();
}