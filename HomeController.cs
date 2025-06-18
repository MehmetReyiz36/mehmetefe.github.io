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
                _logger.LogWarning("�leti�im formu g�nderimi ba�ar�s�z: Bo� email veya mesaj.");
                ViewBag.Error = "L�tfen t�m alanlar� doldurun.";
                return View();
            }

            try
            {
                _logger.LogInformation("�leti�im formu g�nderiliyor: Email={Email}, Mesaj={Message}", email, message);
                bool isSuccess = await _sheetsService.AppendContactDataAsync(email, message);
                if (isSuccess)
                {
                    _logger.LogInformation("�leti�im formu ba�ar�yla g�nderildi: Email={Email}", email);
                    ViewBag.Success = "Mesaj�n�z ba�ar�yla g�nderildi!";
                }
                else
                {
                    _logger.LogError("�leti�im formu g�nderimi ba�ar�s�z: Veri Google Tablo'ya eklenemedi, Email={Email}", email);
                    ViewBag.Error = "Mesaj g�nderilemedi. L�tfen daha sonra tekrar deneyin.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�leti�im formu g�nderimi ba�ar�s�z: Email={Email}, Mesaj={Message}", email, message);
                ViewBag.Error = $"Bir hata olu�tu: {ex.Message}. L�tfen Google Tablo'yu personal-website-service-proje@kendisitem.iam.gserviceaccount.com ile payla��n (D�zenleyici izni ile).";
            }
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            _logger.LogError("Hata sayfas� �a�r�ld�: RequestId={RequestId}", Activity.Current?.Id ?? HttpContext.TraceIdentifier);
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}