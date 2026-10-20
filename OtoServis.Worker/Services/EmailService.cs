using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Polly;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace OtoServis.Worker.Services
{
    public class EmailService : IMailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly IConfiguration _configuration;
        
        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        // SmtpException yerine genel Exception yakalayacak şekilde Polly'i güncelliyoruz
        private AsyncPolicy GetRetryPolicy()
        {
            return Policy
                .Handle<Exception>() 
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning($"E-posta gönderim hatası: {exception.Message}. {timeSpan.TotalSeconds} saniye sonra tekrar denenecek. (Deneme {retryCount}/3)");
                    });
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            await SendViaResendApiAsync(toEmail, subject, message);
        }

        public async Task FaturaMailiGonderAsync(string aliciEmail, string musteriAd, string plaka, decimal iscilik, decimal parca, decimal toplam, string? pdfDosyaYolu = null)
        {
            string subject = $"{plaka} Plakalı Aracınızın Servis Faturası ve E-Faturanız";
            string htmlBody = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #ddd;'>
                    <h2 style='color: #2c3e50;'>Sayın {musteriAd},</h2>
                    <p><strong>{plaka}</strong> plakalı aracınızın servis işlemleri başarıyla tamamlanmıştır. E-Faturanız ekte yer almaktadır.</p>
                    <hr>
                    <table style='width: 100%; text-align: left;'>
                        <tr><th>İşçilik Ücreti:</th><td>{iscilik} ₺</td></tr>
                        <tr><th>Yedek Parça Maliyeti:</th><td>{parca} ₺</td></tr>
                        <tr style='font-size: 1.2em; color: #e74c3c;'><th>Genel Toplam:</th><td><strong>{toplam} ₺</strong></td></tr>
                    </table>
                    <hr>
                    <p>Bizi tercih ettiğiniz için teşekkür ederiz!</p>
                </div>";

            await SendViaResendApiAsync(aliciEmail, subject, htmlBody, pdfDosyaYolu);
            _logger.LogInformation($"[FATURA E-POSTASI ATILDI] -> {aliciEmail}");
        }

        public async Task SifreSifirlamaEpostaGonderAsync(string aliciEmail, string konu, string htmlIcerik)
        {
            await SendViaResendApiAsync(aliciEmail, konu, htmlIcerik);
            _logger.LogInformation($"[ŞİFRE SIFIRLAMA E-POSTASI ATILDI] -> {aliciEmail}");
        }

        // Tüm metotların ortak kullandığı merkezi HTTP API (Resend) gönderim motoru
        private async Task SendViaResendApiAsync(string to, string subject, string htmlContent, string? attachmentPath = null)
        {
            var apiKey = _configuration["SmtpSettings:Password"]; // Resend API Key buraya kayıtlı kabul ediyoruz
            var senderEmail = "onboarding@resend.dev"; // Resend test hesabı için zorunlu gönderici

            var payload = new Dictionary<string, object>
            {
                { "from", $"Oto Servis <{senderEmail}>" },
                { "to", new[] { to } }, // ÖNEMLİ: Bu adres senin Resend'e kayıt olduğun kişisel e-posta adresin olmalı
                { "subject", subject },
                { "html", htmlContent }
            };

            // PDF eki varsa, dosyayı Base64 formatına çevirip JSON'a ekliyoruz
            if (!string.IsNullOrEmpty(attachmentPath) && File.Exists(attachmentPath))
            {
                var fileBytes = await File.ReadAllBytesAsync(attachmentPath);
                var base64File = Convert.ToBase64String(fileBytes);
                var fileName = Path.GetFileName(attachmentPath);

                payload.Add("attachments", new[]
                {
                    new { filename = fileName, content = base64File }
                });
            }

            await GetRetryPolicy().ExecuteAsync(async () =>
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("https://api.resend.com/emails", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Resend API Hatası: {error}");
                }
            });
        }
    }
}
