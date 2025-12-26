using Microsoft.EntityFrameworkCore;
using OnlineShop.Models; 
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
namespace OnlineShop.Data 
{
    public class ApplicationDbContext :  IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Review> Reviews { get; set; }


        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<WishlistItem> WishlistItems { get; set; }


        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }




        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Soft delete: produsele IsDeleted nu apar in queries normale
            modelBuilder.Entity<Product>()
                .HasQueryFilter(p => !p.IsDeleted);
            
            modelBuilder.Entity<Category>()
                .HasQueryFilter(c => !c.IsDeleted);
            modelBuilder.Entity<Product>()
                .HasQueryFilter(p => !p.IsDeleted);

            // Protectie istoric comenzi: nu sterge OrderItem cand se sterge Product
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            //configurarea relatiei Categorie -> Produse
            modelBuilder.Entity<Category>()
                .HasMany(c => c.Products)
                .WithOne(p => p.Category)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            //configurarea relatiei Produs -> Reviewuri
            modelBuilder.Entity<Product>()
                .HasMany(p => p.Reviews)
                .WithOne(r => r.Product)
                .HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.Cascade);






            //configurez WishlistItem sa fie unic per User + Product
            modelBuilder.Entity<WishlistItem>()
                .HasIndex(w => new { w.UserId, w.ProductId })
                .IsUnique();

            //relatia pt CartItem -> User
            modelBuilder.Entity<CartItem>()
                .HasOne(c => c.User)
                .WithMany(u => u.CartItems)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            //relatia pt WishlistItem -> User
            modelBuilder.Entity<WishlistItem>()
                .HasOne(w => w.User)
                .WithMany(u => u.WishlistItems)
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            //relatie CartItem -> Product
            modelBuilder.Entity<CartItem>()
                .HasOne(c => c.Product)
                .WithMany()
                .HasForeignKey(c => c.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            //relatie WishlistItem -> Product
            modelBuilder.Entity<WishlistItem>()
                .HasOne(w => w.Product)
                .WithMany()
                .HasForeignKey(w => w.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        
    }
    }
}