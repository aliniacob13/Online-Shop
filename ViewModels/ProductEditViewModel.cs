namespace OnlineShop.ViewModels
{
    public class ProductEditViewModel
    {
        public int Id { get; set; }

        // toate optionale, ca sa le poti lasa goale
        public string? Title { get; set; }
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public int? Stock { get; set; }
        public int? CategoryId { get; set; }

        // imagine noua optionala
        public IFormFile? ImagineNoua { get; set; }

        // pentru afisat imaginea veche in view
        public string? ImageUrlCurenta { get; set; }
    }
}