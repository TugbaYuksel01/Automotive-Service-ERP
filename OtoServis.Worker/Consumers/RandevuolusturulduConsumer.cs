using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Configuration; // YENİ EKLENDİ
using Microsoft.Extensions.Logging;
using OtoServis.Contracts.Events;
using OtoServis.Worker.Services;

namespace OtoServis.Worker.Consumers
{
    public class RandevuOlusturulduConsumer : IConsumer<RandevuOlusturulduEvent>
    {
        private readonly IMailService _mailService;
        private readonly ILogger<RandevuOlusturulduConsumer> _logger;
        private readonly IConfiguration _configuration; // YENİ EKLENDİ

        // Constructor'a IConfiguration inject ettik
        public RandevuOlusturulduConsumer(
            IMailService mailService, 
            ILogger<RandevuOlusturulduConsumer> logger,
            IConfiguration configuration)
        {
            _mailService = mailService;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task Consume(ConsumeContext<RandevuOlusturulduEvent> context)
        {
            var data = context.Message;
            _logger.LogInformation("Worker: Randevu #{RandevuId} kuyruktan alındı, mail hazırlanıyor...", data.RandevuId);

            // ESKİ HALİ: string frontendUrl = "http://localhost:5173";
            // YENİ HALİ: AppSettings veya Environment Variables üzerinden oku, bulamazsan localhost'a düş
            string frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:5173";

            // --- DİNAMİK MANTIK BURADA ÇALIŞIYOR ---
            string butonLinki = data.IsMisafir
                ? $"{frontendUrl}/takip?id={data.RandevuId}"
                : $"{frontendUrl}/randevularim"; // (Kayıtlı müşterinin kendi paneli)

            string butonMetni = data.IsMisafir
                ? "Aracımın Durumunu Takip Et (Şifresiz)"
                : "Randevularımı Görüntüle";

            string yonlendirmeMetni = data.IsMisafir
                ? "Sisteme üye olmadan aracınızın anlık servis durumunu aşağıdaki butona tıklayarak güvenle takip edebilirsiniz:"
                : "Müşteri panelinize giriş yaparak randevu detaylarınızı ve geçmiş işlemlerinizi kontrol edebilirsiniz:";

            string konu = $"Servis Randevunuz Onaylandı (#{data.RandevuId})";

            // Şık Kurumsal Mail Şablonu
            string icerik = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; border: 1px solid #e5e7eb; border-radius: 8px; padding: 20px;'>
                    <h2 style='color: #4F46E5;'>OtoServis Pro</h2>
                    <p>Sayın <strong>{data.MusteriAdSoyad}</strong>,</p>
                    <p><strong>{data.AracPlaka}</strong> plakalı aracınız için <strong>{data.RandevuTarihi:dd.MM.yyyy HH:mm}</strong> tarihli servis randevunuz başarıyla onaylanmıştır.</p>
                    
                    <div style='background-color: #f3f4f6; padding: 15px; border-radius: 6px; margin: 20px 0; text-align: center;'>
                        <p style='margin: 0; font-size: 14px; color: #6b7280;'>Randevu Numarası</p>
                        <p style='margin: 5px 0 0 0; font-size: 24px; font-weight: bold; color: #111827;'>#{data.RandevuId}</p>
                    </div>

                    <p>{yonlendirmeMetni}</p>
                    
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{butonLinki}' style='background-color: #4F46E5; color: white; padding: 14px 28px; text-decoration: none; border-radius: 6px; font-weight: bold; display: inline-block;'>
                            {butonMetni}
                        </a>
                    </div>

                    <p style='margin-top: 30px; font-size: 12px; color: #6b7280; text-align: center;'>
                        İyi yolculuklar dileriz.<br>OtoServis Pro Ekibi
                    </p>
                </div>";

            // Hazırlanan mailleri yolluyoruz
            await _mailService.SendEmailAsync(data.MusteriEmail, konu, icerik);

            _logger.LogInformation("Worker: Randevu #{RandevuId} onay maili {Email} adresine başarıyla fırlatıldı.", data.RandevuId, data.MusteriEmail);
        }
    }
}
