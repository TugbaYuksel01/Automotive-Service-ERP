using OtoServis.Data.Models;

namespace OtoServisApi.Services
{
    public interface IPdfFaturaService
    {
        // Geriye oluşturulan PDF'in diskteki dosya yolunu (string) dönecek
        Task<string> FaturaUretVeKaydetAsync(Fatura faturaDetayi, Randevu randevu, List<YedekParca> kullanilanParcalar);
    }
}
