using OtoServis.Data.Models;

namespace OtoServis.Data.Repositories
{
    public interface IRandevuRepository : IRepository<Randevu>
    {
        // Standart CRUD zaten IRepository'den geliyor.
        // Biz sadece Randevuya özel, ilişkili verileri (Include ile) çeken metotları imzalıyoruz.
        Task<IEnumerable<Randevu>> TumRandevulariIliskileriyleGetirAsync();
        Task<IEnumerable<Randevu>> UstaninGunlukRandevulariniGetirAsync(int ustaId, DateTime tarih);
    }
}
