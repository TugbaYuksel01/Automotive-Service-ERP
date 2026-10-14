using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OtoServis.Worker.Services
{
    public interface IMailService
    {
        Task SendEmailAsync(string toEmail, string subject, string message);
        Task FaturaMailiGonderAsync(string aliciEmail, string musteriAd, string plaka, decimal iscilik, decimal parca, decimal toplam,string? pdfDosyaYolu = null);
        Task SifreSifirlamaEpostaGonderAsync(string aliciEmail, string konu, string htmlIcerik);
    }
}
