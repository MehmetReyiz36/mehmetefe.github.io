using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WEB.Services;
using System.Diagnostics;
using WEB.Models;

namespace WEB.Controllers
{
    public class HomeController : Controller
    {
        private readonly GoogleSheetsService _sheetsService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(GoogleSheetsService sheetsService, ILogger<HomeController> logger)
        {
            _sheetsService = sheetsService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Projects()
        {
            return View();
        }

        public IActionResult CV()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Contact()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Contact(string email, string message)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(message))
            {
                _logger.LogWarning("Ýletiþim formu gönderimi baþarýsýz: Boþ email veya mesaj.");
                ViewBag.Error = "Lütfen tüm alanlarý doldurun.";
                return View();
            }

            try
            {
                _logger.LogInformation("Ýletiþim formu gönderiliyor: Email={Email}, Mesaj={Message}", email, message);
                bool isSuccess = await _sheetsService.AppendContactDataAsync(email, message);
                if (isSuccess)
                {
                    _logger.LogInformation("Ýletiþim formu baþarýyla gönderildi: Email={Email}", email);
                    ViewBag.Success = "Mesajýnýz baþarýyla gönderildi!";
                }
                else
                {
                    _logger.LogError("Ýletiþim formu gönderimi baþarýsýz: Veri Google Tablo'ya eklenemedi, Email={Email}", email);
                    ViewBag.Error = "Mesaj gönderilemedi. Lütfen daha sonra tekrar deneyin.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ýletiþim formu gönderimi baþarýsýz: Email={Email}, Mesaj={Message}", email, message);
                ViewBag.Error = $"Bir hata oluþtu: {ex.Message}. Lütfen Google Tablo'yu personal-website-service-proje@kendisitem.iam.gserviceaccount.com ile paylaþýn (Düzenleyici izni ile).";
            }
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            _logger.LogError("Hata sayfasý çaðrýldý: RequestId={RequestId}", Activity.Current?.Id ?? HttpContext.TraceIdentifier);
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}