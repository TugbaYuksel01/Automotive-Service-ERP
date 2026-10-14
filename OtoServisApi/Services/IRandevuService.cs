using OtoServisApi.DTOs;
using OtoServis.Data.Models;


namespace OtoServisApi.Services
{
    public interface IRandevuService
    {
        Task<IEnumerable<RandevuDetayDto>> TumRandevulariGetirAsync();

        // Temel İşlem

        Task<int> RandevuOlusturAsync(RandevuOlusturDto dto);

        // Yeni İş Kuralları İçin Metotlar
        Task<bool> RandevuIptalEtAsync(int randevuId);

        Task<IEnumerable<Randevu>> GetMusteriRandevulariAsync(int musteriId);
        Task<bool> RandevuTamamlaAsync(int randevuId);

        Task<bool> SureUzatVeArkadakileriKaydirAsync(int randevuId, SureUzatDto dto);

        Task<bool> RandevuBaslatAsync(int randevuId);
        Task<Fatura> IslemiTamamlaVeFaturaKesAsync(RandevuTamamlamaDto dto);
        Task<IEnumerable<Randevu>> GetUstaninGunlukRandevulariAsync(int ustaId);
        Task<IEnumerable<Randevu>> GetGunlukTumRandevularAsync();
        Task AciliyetVeKapasiteKontroluAsync(RandevuOlusturDto dto);
    }
}