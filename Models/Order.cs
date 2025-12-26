using System;
using System.Collections.Generic;

namespace OnlineShop.Models
{
    public class Order
    {
        public int Id { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;

        public ICollection<OrderItem> OrderItems { get; set; }
    }
}
