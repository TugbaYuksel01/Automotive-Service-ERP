using System;
using System.Collections.Generic;
using MassTransit;
using OtoServis.Contracts.Events;
using OtoServis.Worker.Services;

namespace OtoServis.Worker.Consumers
{
    public class FaturaKesildiConsumer : IConsumer<FaturaKesildiEvent>
    {
        private readonly IMailService _mailService;
        private readonly ILogger<FaturaKesildiConsumer> _logger;

        public FaturaKesildiConsumer(IMailService mailService, ILogger<FaturaKesildiConsumer> logger)
        {
            _mailService = mailService;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<FaturaKesildiEvent> context)
        {
            var data = context.Message;
            _logger.LogInformation("Worker: Fatura #{FaturaId} kuyruktan alındı. Fatura maili gönderiliyor...", data.FaturaId);
                       

           await _mailService.FaturaMailiGonderAsync(data.MusteriEmail, data.MusteriAdSoyad, data.AracPlaka, data.IscilikUcreti,data.Parca, data.ToplamTutar, data.PdfDosyaYolu);

            _logger.LogInformation("Worker: Fatura #{FaturaId} maili {Email} adresine başarıyla iletildi.", data.FaturaId, data.MusteriEmail);
        }
    }
}
