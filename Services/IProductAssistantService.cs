namespace OnlineShop.Services
{
    public class ProductAssistantResult
    {
        public bool Success { get; set; }
        public string Answer { get; set; } = "Momentan nu avem detalii despre acest aspect.";
        public int? MatchedFaqId { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public interface IProductAssistantService
    {
        Task<ProductAssistantResult> AskAsync(int productId, string intrebare, string? userId);
    }
}