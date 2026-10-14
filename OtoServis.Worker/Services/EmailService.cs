using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Polly;
using System.Net.Mail;
using System.Net;

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

        // Polly Politikasını merkezi olarak tanımlıyoruz ki her iki metotta da kullanabilelim
        private AsyncPolicy GetRetryPolicy()
        {
            return Policy
                .Handle<SmtpException>()
                .Or<IOException>()
                .Or<TimeoutException>()
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
            var smtpAyarlari = _configuration.GetSection("SmtpSettings");

            var gondericiEmail = smtpAyarlari["SenderEmail"];
            var sifre = smtpAyarlari["Password"];
            var sunucu = smtpAyarlari["Server"];
            var port = int.Parse(smtpAyarlari["Port"]!);

            var mailMessage = new MailMessage
            {
                From = new MailAddress(gondericiEmail!, smtpAyarlari["SenderName"]),
                Subject = subject,
                Body = message,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            // Polly kalkanı ile sarmalanmış gönderim
            await GetRetryPolicy().ExecuteAsync(async () =>
            {
                using var client = new SmtpClient(sunucu, port)
                {
                    Credentials = new NetworkCredential(gondericiEmail, sifre),
                    EnableSsl = true
                };

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation($"[GERÇEK E-POSTA ATILDI] -> {toEmail}");
            });
        }

        public async Task FaturaMailiGonderAsync(string aliciEmail, string musteriAd, string plaka, decimal iscilik, decimal parca, decimal toplam, string? pdfDosyaYolu = null)
        {
            var smtpAyarlari = _configuration.GetSection("SmtpSettings");

            var mail = new MailMessage
            {
                From = new MailAddress(smtpAyarlari["SenderEmail"], smtpAyarlari["SenderName"]),
                Subject = $"{plaka} Plakalı Aracınızın Servis Faturası ve E-Faturanız",
                IsBodyHtml = true
            };
            mail.To.Add(aliciEmail);

            // Şık HTML fatura gövdesi
            mail.Body = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #ddd;'>
                    <h2 style='color: #2c3e50;'>Sayın {musteriAd},</h2>
                    <p><strong>{plaka}</strong> plakalı aracınızın servis işlemleri başarıyla tamamlanmıştır. E-Faturanız ekteki PDF dosyasında yer almaktadır.</p>
                    <hr>
                    <table style='width: 100%; text-align: left;'>
                        <tr><th>İşçilik Ücreti:</th><td>{iscilik} ₺</td></tr>
                        <tr><th>Yedek Parça Maliyeti:</th><td>{parca} ₺</td></tr>
                        <tr style='font-size: 1.2em; color: #e74c3c;'><th>Genel Toplam:</th><td><strong>{toplam} ₺</strong></td></tr>
                    </table>
                    <hr>
                    <p>Bizi tercih ettiğiniz için teşekkür ederiz!</p>
                </div>";

            // EĞER OLUŞTURULMUŞ BİR PDF DOSYASI VARSA MAİLE EKLE (ATTACHMENT)
            if (!string.IsNullOrEmpty(pdfDosyaYolu) && File.Exists(pdfDosyaYolu))
            {
                var attachment = new Attachment(pdfDosyaYolu);
                mail.Attachments.Add(attachment);
            }

            // Polly kalkanı ile sarmalanmış gönderim
            await GetRetryPolicy().ExecuteAsync(async () =>
            {
                using var client = new SmtpClient(smtpAyarlari["Server"], int.Parse(smtpAyarlari["Port"]!))
                {
                    Credentials = new NetworkCredential(smtpAyarlari["Username"], smtpAyarlari["Password"]),
                    EnableSsl = true
                };

                await client.SendMailAsync(mail);
                _logger.LogInformation($"[FATURA E-POSTASI ATILDI] -> {aliciEmail}");
            });
                    
        }

        public async Task SifreSifirlamaEpostaGonderAsync(string aliciEmail, string konu, string htmlIcerik)
        {
            var gondericiEmail = _configuration["SmtpSettings:SenderEmail"];
            var sifre = _configuration["SmtpSettings:Password"];

            // using kullanıyoruz ki mail atıldıktan hemen sonra bu nesneler RAM'den silinsin
            using var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(gondericiEmail, sifre),
                EnableSsl = true,
            };

            // Polly kalkanı ile sarmalanmış gönderim
            await GetRetryPolicy().ExecuteAsync(async () =>
            {
                using var mailMessage = new MailMessage
            {
                From = new MailAddress(gondericiEmail, "Oto Servis Sistemi"),
                Subject = konu,
                Body = htmlIcerik,
                IsBodyHtml = true,
            };

            mailMessage.To.Add(aliciEmail);

            await smtpClient.SendMailAsync(mailMessage);
            });
        }

    }
}
