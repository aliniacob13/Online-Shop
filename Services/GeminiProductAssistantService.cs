using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Data;
using OnlineShop.Models;

namespace OnlineShop.Services
{
    public class GeminiProductAssistantService : IProductAssistantService
    {
        private readonly ApplicationDbContext db;
        private readonly HttpClient http;
        private readonly string apiKey;
        private readonly string model;
        private readonly ILogger<GeminiProductAssistantService> logger;

        private const string Fallback = "Momentan nu avem detalii despre acest aspect.";

        public GeminiProductAssistantService(
            ApplicationDbContext context,
            IConfiguration config,
            ILogger<GeminiProductAssistantService> log,
            HttpClient client)
        {
            db = context;
            logger = log;

            // 1) ia din .env / environment variable
            apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                     ?? throw new ArgumentNullException("GEMINI_API_KEY not configured (missing in .env / environment)");

            // 2) model poate rămâne în appsettings (nu e secret)
            model = config["Gemini:Model"] ?? "gemini-2.5-flash";

            // 3) folosim HttpClient din DI
            http = client;
            http.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
        }

        public async Task<ProductAssistantResult> AskAsync(int productId, string intrebare, string? userId)
        {
            intrebare = (intrebare ?? "").Trim();
            if (string.IsNullOrWhiteSpace(intrebare))
                return new ProductAssistantResult { Success = false, Answer = Fallback, ErrorMessage = "Intrebare goala" };

            var produs = await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productId);
            if (produs == null)
                return new ProductAssistantResult { Success = false, Answer = Fallback, ErrorMessage = "Produs inexistent" };

            var faqs = await db.ProductFaqs.AsNoTracking()
                .Where(f => f.ProductId == productId && f.IsApproved)
                .OrderByDescending(f => f.Id)
                .Take(30)
                .ToListAsync();

            // 1) Mai intai, incercam raspuns direct din FAQ (gratis, fara apel AI)
            var faqPotrivit = GasesteFaqPotrivit(intrebare, faqs);
            if (faqPotrivit != null)
            {
                await SalveazaLog(productId, userId, intrebare, faqPotrivit.Answer, faqPotrivit.Id);
                return new ProductAssistantResult { Success = true, Answer = faqPotrivit.Answer, MatchedFaqId = faqPotrivit.Id };
            }

            var contextText = ConstruiesteContext(produs, faqs);

            var instructiuni =
$@"Esti Product Assistant pentru un magazin online.
Raspunde in romana, clar si scurt.
Foloseste STRICT doar informatiile din CONTEXT.
Daca raspunsul nu se gaseste in CONTEXT, raspunde exact: ""{Fallback}"".
Nu inventa detalii.

CONTEXT:
{contextText}

INTREBARE:
{intrebare}";

            try
            {
                var url = $"v1beta/models/{model}:generateContent";

                var body = new
                {
                    contents = new[]
                    {
                        new
                        {
                            role = "user",
                            parts = new[] { new { text = instructiuni } }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.2,
                        maxOutputTokens = 200
                    }
                };

                var json = JsonSerializer.Serialize(body);
                var req = new HttpRequestMessage(HttpMethod.Post, url);
                req.Headers.Add("x-goog-api-key", apiKey);
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var resp = await http.SendAsync(req);
                var respText = await resp.Content.ReadAsStringAsync();

                if (!resp.IsSuccessStatusCode)
                {
                    logger.LogError("Gemini error {StatusCode}: {Body}", resp.StatusCode, respText);
                    await SalveazaLog(productId, userId, intrebare, Fallback, null);
                    return new ProductAssistantResult { Success = false, Answer = Fallback, ErrorMessage = $"API error: {resp.StatusCode}" };
                }

                var raspuns = ExtrageText(respText);
                if (string.IsNullOrWhiteSpace(raspuns))
                    raspuns = Fallback;

                // Fortam fallback daca modelul a ignorat instructiunile
                if (!EsteRaspunsAcceptabil(raspuns))
                    raspuns = Fallback;

                await SalveazaLog(productId, userId, intrebare, raspuns, null);
                return new ProductAssistantResult { Success = true, Answer = raspuns };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Eroare GeminiProductAssistantService");
                await SalveazaLog(productId, userId, intrebare, Fallback, null);
                return new ProductAssistantResult { Success = false, Answer = Fallback, ErrorMessage = ex.Message };
            }
        }

        private static string ConstruiesteContext(Product produs, List<ProductFaq> faqs)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Titlu: {produs.Title}");
            sb.AppendLine("Descriere:");
            sb.AppendLine(produs.Description ?? "");
            sb.AppendLine();

            if (faqs.Count > 0)
            {
                sb.AppendLine("FAQ:");
                foreach (var f in faqs)
                {
                    sb.AppendLine($"- Q: {f.Question}");
                    sb.AppendLine($"  A: {f.Answer}");
                }
            }
            else
            {
                sb.AppendLine("FAQ: (nu exista inca)");
            }

            return sb.ToString();
        }

        private static ProductFaq? GasesteFaqPotrivit(string intrebare, List<ProductFaq> faqs)
        {
            var q = intrebare.ToLowerInvariant().Trim();

            foreach (var f in faqs)
            {
                var fq = (f.Question ?? "").ToLowerInvariant().Trim();
                if (fq.Length >= 6 && q.Contains(fq)) return f;
            }

            var cuvinte = q.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(x => x.Length >= 4)
                .Take(8)
                .ToList();

            foreach (var f in faqs)
            {
                var text = ((f.Question ?? "") + " " + (f.Answer ?? "")).ToLowerInvariant();
                var scor = cuvinte.Count(k => text.Contains(k));
                if (scor >= 3) return f;
            }

            return null;
        }

        private static bool EsteRaspunsAcceptabil(string raspuns)
        {
            if (string.IsNullOrWhiteSpace(raspuns)) return false;
            if (raspuns.Length > 2000) return false;
            return true;
        }

        private static string? ExtrageText(string respText)
        {
            using var doc = JsonDocument.Parse(respText);

            if (!doc.RootElement.TryGetProperty("candidates", out var candidates)) return null;
            if (candidates.GetArrayLength() == 0) return null;

            var cand0 = candidates[0];
            if (!cand0.TryGetProperty("content", out var content)) return null;
            if (!content.TryGetProperty("parts", out var parts)) return null;
            if (parts.GetArrayLength() == 0) return null;

            var sb = new StringBuilder();

            foreach (var p in parts.EnumerateArray())
            {
                if (p.TryGetProperty("text", out var t))
                {
                    sb.Append(t.GetString());
                }
            }

            var text = sb.ToString().Trim();
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }

        private async Task SalveazaLog(int productId, string? userId, string intrebare, string raspuns, int? matchedFaqId)
        {
            db.ProductQuestionLogs.Add(new ProductQuestionLog
            {
                ProductId = productId,
                UserId = userId,
                Question = intrebare,
                AssistantAnswer = raspuns,
                MatchedFaqId = matchedFaqId,
                AskedAt = DateTime.Now
            });

            await db.SaveChangesAsync();
        }
    }
}