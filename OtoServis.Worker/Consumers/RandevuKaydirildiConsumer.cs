using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MassTransit;
using OtoServis.Contracts.Events;
using OtoServis.Worker.Services;

namespace OtoServis.Worker.Consumers
{
    public class RandevuKaydirildiConsumer : IConsumer<RandevuKaydirildiEvent>
    {
        private readonly IMailService _mailService;
        private readonly ILogger<RandevuKaydirildiConsumer> _logger;

        public RandevuKaydirildiConsumer(IMailService mailService, ILogger<RandevuKaydirildiConsumer> logger)
        {
            _mailService = mailService;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<RandevuKaydirildiEvent> context)
        {
            var data = context.Message;
            _logger.LogWarning("Worker: Randevu #{RandevuId} için {Tip} bildirimi gönderiliyor...", data.RandevuId, data.Tip);

            string konu = data.Tip switch
            {
                KaydirmaTipi.VipErtesiGun => "Önemli: Randevunuz VIP Olarak Yarın Sabaha Güncellendi",
                KaydirmaTipi.KrizErtelemesi => "Bilgilendirme: Servis Randevunuzda Zorunlu Erteleme",
                _ => "Servis Randevu Saatinizde Güncelleme"
            };

            string icerik = $@"
                <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <h2>Sayın {data.MusteriAdSoyad},</h2>
                    <p><strong>{data.AracPlaka}</strong> plakalı aracınızın servis planlamasında operasyonel durumlar nedeniyle değişiklik yapılmıştır.</p>
                    
                    <div style='background-color: #f8f9fa; border-left: 4px solid #ff9800; padding: 12px; margin: 15px 0;'>
                        <p style='margin: 0;'><strong>Eski Randevu Saati:</strong> {data.EskiTarih:dd.MM.yyyy HH:mm}</p>
                        <p style='margin: 0; color: #d9534f;'><strong>Yeni Randevu Saati:</strong> {data.YeniTarih:dd.MM.yyyy HH:mm}</p>
                    </div>

                    <p><strong>Açıklama:</strong> {data.Aciklama}</p>
                    {(data.Tip == KaydirmaTipi.VipErtesiGun ? "<p><strong>Not:</strong> Aracınız servisimize ulaştığında bekleme yapılmadan doğrudan 'VIP İlk Araç' olarak işleme alınacaktır.</p>" : "")}
                    <p>Yaşanan aksaklık sebebiyle özür diler, anlayışınız için teşekkür ederiz.</p>
                </div>";

            await _mailService.SendEmailAsync(data.MusteriEmail, konu, icerik);

            _logger.LogInformation("Worker: #{RandevuId} nolu randevu güncelleme maili başarıyla gönderildi.", data.RandevuId);
        }
    }
}
