using System.ComponentModel.DataAnnotations;

namespace OnlineShop.Models
{
    public class WishlistItem
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        [Required]
        public int ProductId { get; set; }
        public Product Product { get; set; }
    }
}
