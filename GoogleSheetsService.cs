using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace WEB.Services
{
    public class GoogleSheetsService
    {
        private readonly SheetsService _sheetsService;
        private readonly string _spreadsheetId;
        private readonly string _sheetName = "ContactForm";
        private readonly ILogger<GoogleSheetsService> _logger;

        public GoogleSheetsService(IConfiguration configuration, ILogger<GoogleSheetsService> logger)
        {
            _logger = logger;

            _spreadsheetId = configuration["GoogleSheets:SpreadsheetId"]
                ?? throw new ArgumentNullException("GoogleSheets:SpreadsheetId appsettings.json'de eksik.");

            var credentialFilePath = configuration["GoogleSheets:CredentialFilePath"] ?? "credentials.json";
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), credentialFilePath);

            if (!File.Exists(fullPath))
            {
                _logger.LogError("Kimlik dosyası bulunamadı: {Path}", fullPath);
                throw new FileNotFoundException($"Kimlik dosyası bulunamadı: {fullPath}. Dosyanın var olduğundan ve appsettings.json'deki yolun doğru olduğundan emin olun.");
            }

            try
            {
                _logger.LogInformation("Kimlik dosyası yükleniyor: {Path}", fullPath);
                using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
                var credential = GoogleCredential.FromStream(stream)
                    .CreateScoped(SheetsService.Scope.Spreadsheets);

                _sheetsService = new SheetsService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "WEB"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Google Sheets servisi başlatılamadı");
                throw new InvalidOperationException($"Google Sheets servisi başlatılamadı: {ex.Message}", ex);
            }
        }

        public async Task<bool> AppendContactDataAsync(string email, string message)
        {
            try
            {
                _logger.LogInformation("Google Tablo'ya veri ekleniyor: Email={Email}, Mesaj={Message}, Tablo={SheetName}, Range={Range}, SpreadsheetId={SpreadsheetId}",
                    email, message, _sheetName, $"{_sheetName}!A:C", _spreadsheetId);

                // Tabloya erişimi test et
                var spreadsheet = await _sheetsService.Spreadsheets.Get(_spreadsheetId).ExecuteAsync();
                _logger.LogInformation("Tablo erişimi başarılı: Title={Title}, SheetCount={SheetCount}",
                    spreadsheet.Properties.Title, spreadsheet.Sheets.Count);

                // ContactForm sayfasını kontrol et
                var sheet = spreadsheet.Sheets.FirstOrDefault(s => s.Properties.Title == _sheetName);
                if (sheet == null)
                {
                    _logger.LogWarning("ContactForm sayfası bulunamadı, yeni sayfa oluşturuluyor: SpreadsheetId={SpreadsheetId}", _spreadsheetId);

                    // Yeni sayfa oluştur
                    var addSheetRequest = new Request
                    {
                        AddSheet = new AddSheetRequest
                        {
                            Properties = new SheetProperties
                            {
                                Title = _sheetName
                            }
                        }
                    };
                    var batchUpdateRequest = new BatchUpdateSpreadsheetRequest
                    {
                        Requests = new List<Request> { addSheetRequest }
                    };
                    var batchResponse = await _sheetsService.Spreadsheets.BatchUpdate(batchUpdateRequest, _spreadsheetId).ExecuteAsync();
                    _logger.LogInformation("ContactForm sayfası başarıyla oluşturuldu: SpreadsheetId={SpreadsheetId}, ResponseCount={ResponseCount}",
                        _spreadsheetId, batchResponse.Replies?.Count);

                    // Başlıkları ekle
                    var headers = new ValueRange
                    {
                        Values = new List<IList<object>> { new List<object> { "Zaman Damgası", "Gmail", "Mesaj" } }
                    };
                    var headerRange = $"{_sheetName}!A1:C1";
                    var updateRequest = _sheetsService.Spreadsheets.Values.Update(headers, _spreadsheetId, headerRange);
                    updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                    var headerResponse = await updateRequest.ExecuteAsync();
                    _logger.LogInformation("Başlıklar eklendi: Range={Range}, UpdatedCells={UpdatedCells}",
                        headerRange, headerResponse.UpdatedCells);
                }
                else
                {
                    _logger.LogInformation("ContactForm sayfası bulundu: SheetId={SheetId}", sheet.Properties.SheetId);
                }

                // Veri ekle
                var range = $"{_sheetName}!A:C";
                var valueRange = new ValueRange
                {
                    Values = new List<IList<object>>
                    {
                        new List<object> { DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), email, message }
                    }
                };

                var appendRequest = _sheetsService.Spreadsheets.Values.Append(valueRange, _spreadsheetId, range);
                appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
                var response = await appendRequest.ExecuteAsync();

                // Veri ekleme başarısını doğrula
                if (response.Updates?.UpdatedRows > 0)
                {
                    _logger.LogInformation("Veri Google Tablo'ya başarıyla eklendi: SpreadsheetId={SpreadsheetId}, SheetName={SheetName}, UpdatedRange={UpdatedRange}, UpdatedRows={UpdatedRows}",
                        _spreadsheetId, _sheetName, response.Updates?.UpdatedRange, response.Updates?.UpdatedRows);
                    return true;
                }
                else
                {
                    _logger.LogError("Veri eklenemedi: UpdatedRows=0, SpreadsheetId={SpreadsheetId}, SheetName={SheetName}",
                        _spreadsheetId, _sheetName);
                    throw new InvalidOperationException("Veri eklenemedi: Google Tablo güncellenmedi (UpdatedRows=0).");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Google Tablo'ya veri eklenemedi: SpreadsheetId={SpreadsheetId}, SheetName={SheetName}, Range={Range}",
                    _spreadsheetId, _sheetName, $"{_sheetName}!A:C");
                throw new InvalidOperationException($"Veri eklenemedi: {ex.Message}. Servis hesabı izinlerini kontrol edin: personal-website-service-proje@kendisitem.iam.gserviceaccount.com", ex);
            }
        }
    }
}