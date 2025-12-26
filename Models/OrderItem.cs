using System.ComponentModel.DataAnnotations.Schema;
namespace OnlineShop.Models
{
    public class OrderItem
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }

        public int Quantity { get; set; }
        
        
        //Snapshot
        public string ProductTitleAtPurchase { get; set; } = "";
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPriceAtPurchase { get; set; }

        public string? ImageUrlAtPurchase { get; set; }

        [NotMapped]
        public decimal LineTotal => UnitPriceAtPurchase * Quantity;
    }
}
