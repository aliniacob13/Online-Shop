using System.ComponentModel.DataAnnotations;

namespace OnlineShop.Models
{
    public class ProductQuestionLog
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }
        public Product Product { get; set; }

        public string? UserId { get; set; } // null daca e vizitator

        [Required, StringLength(500)]
        public string Question { get; set; } = "";

        [Required, StringLength(2000)]
        public string AssistantAnswer { get; set; } = "";

        public DateTime AskedAt { get; set; } = DateTime.Now;

        public int? MatchedFaqId { get; set; }
        public ProductFaq? MatchedFaq { get; set; }
    }
}