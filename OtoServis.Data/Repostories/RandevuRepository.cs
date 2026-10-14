using Microsoft.EntityFrameworkCore;
using OtoServis.Data;
using OtoServis.Data.Models;

namespace OtoServis.Data.Repositories
{
    // Hem temel Repository özelliklerini alır hem de IRandevuRepository sözleşmesini uygular
    public class RandevuRepository : Repository<Randevu>, IRandevuRepository
    {
        public RandevuRepository(ServisDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Randevu>> TumRandevulariIliskileriyleGetirAsync()
        {
            // Eskiden Service içinde olan o karmaşık veritabanı sorgusu artık buraya hapsoldu!
            return await _context.Randevular
                .Include(r => r.Kullanici)
                .Include(r => r.HizmetTipi)
                .Include(r => r.Usta)
                .Include(r => r.Arac)
                .ToListAsync();
        }

        public async Task<IEnumerable<Randevu>> UstaninGunlukRandevulariniGetirAsync(int ustaId, DateTime tarih)
        {
            // SQL'e "Sadece bu ustanın işlerini ve sadece yıl/ay/gün olarak eşleşenleri getir" diyoruz.
            // Saat, dakika, saniye farkları (00:00:00 vs 16:30:00) tamamen göz ardı edilecek!
            return await _context.Randevular
                .Include(r => r.Kullanici)
                .Include(r => r.HizmetTipi)
                .Include(r => r.Arac)
                .Where(r => r.UstaId == ustaId
                         && r.Tarih.Year == tarih.Year
                         && r.Tarih.Month == tarih.Month
                         && r.Tarih.Day == tarih.Day)
                .OrderBy(r => r.Tarih)
                .ToListAsync();
        }
    }
}