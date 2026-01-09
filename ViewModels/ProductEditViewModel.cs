using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
namespace OnlineShop.ViewModels
{
    public class ProductEditViewModel
    {
        public int Id { get; set; }

        // toate optionale
        public string? Title { get; set; }
        public string? Description { get; set; }
        
        [Range(0.01, double.MaxValue, ErrorMessage = "Prețul trebuie să fie mai mare decât 0.")]
        public decimal? Price { get; set; }
        
        [Range(0, int.MaxValue, ErrorMessage = "Stocul nu poate fi negativ.")]
        public int? Stock { get; set; }
        public int? CategoryId { get; set; }

        // imagine noua optionala
        public IFormFile? ImagineNoua { get; set; }

        // pentru afisat imaginea veche in view
        public string? ImageUrlCurenta { get; set; }
    }
}