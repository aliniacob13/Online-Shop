namespace OnlineShop.ViewModels.AdminTopQuestions
{
    public class TopQVm
    {
        public int ProductId { get; set; }
        public string ProductTitle { get; set; } = "";
        public string Question { get; set; } = "";
        public int Count { get; set; }
        public DateTime LastAsked { get; set; }
    }
}