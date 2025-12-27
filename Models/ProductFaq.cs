using System.ComponentModel.DataAnnotations;

namespace OnlineShop.Models
{
    public class ProductFaq
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }
        public Product Product { get; set; }

        [Required, StringLength(200)]
        public string Question { get; set; } = "";

        [Required, StringLength(1000)]
        public string Answer { get; set; } = "";

        public bool IsApproved { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}