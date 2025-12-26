using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace OnlineShop.Models;
public class Review : IValidatableObject
{
    public int Id { get; set; }

    //rating de la 1 la 5
    [Range(1, 5, ErrorMessage = "Ratingul trebuie să fie între 1 și 5.")]
    public int? Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime DatePosted { get; set; } = DateTime.Now;

    //RELATII
    public int ProductId { get; set; }
    
    public string? UserId { get; set; }
    public virtual ApplicationUser? User { get; set; }
    [ValidateNever]
    public Product? Product { get; set; }

    //validare customizata
    //aceasta metoda este apelata automat la validarea modelului
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        //daca nu exista nici rating, nici comentariu -> eroare
        if (Rating == null && string.IsNullOrWhiteSpace(Comment))
        {
            yield return new ValidationResult(
                "Nu se poate posta un review gol, trebuie cel putin adaugata o nota sau un comentariu."
            );
        }
    }
}