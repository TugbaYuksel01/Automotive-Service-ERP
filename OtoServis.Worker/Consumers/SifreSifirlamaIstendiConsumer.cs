using MassTransit;
using OtoServis.Data.Contracts;
using OtoServis.Worker.Services;
using System;
using System.Threading.Tasks;

namespace OtoServis.Worker.Consumers
{
    public class SifreSifirlamaIstendiConsumer : IConsumer<SifreSifirlamaIstendiEvent>
    {
        private readonly IMailService _emailService;

        // Dependency Injection ile Worker'ın kendi Email servisini alıyoruz
        public SifreSifirlamaIstendiConsumer(IMailService emailService)
        {
            _emailService = emailService;
        }

        public async Task Consume(ConsumeContext<SifreSifirlamaIstendiEvent> context)
        {
            var gelenMesaj = context.Message;
            Console.WriteLine($"[RabbitMQ] E-Posta gönderim görevi alındı. Alıcı: {gelenMesaj.AliciEmail}");

            try
            {
                // İşlemi temiz email servisimize paslıyoruz
                await _emailService.SifreSifirlamaEpostaGonderAsync(
                    gelenMesaj.AliciEmail,
                    gelenMesaj.Konu,
                    gelenMesaj.HtmlIcerik);

                Console.WriteLine($"[Başarılı] E-Posta başarıyla fırlatıldı!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HATA] E-Posta gönderilemedi: {ex.Message}");
                // throw ex dersek MassTransit hatayı yakalar ve mesajı kaybetmeyip birazdan tekrar dener!
                throw;
            }
        }
    }
}
