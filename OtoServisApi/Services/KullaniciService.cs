using OtoServis.Data.Models;
using OtoServis.Data.Repositories;

namespace OtoServisApi.Services
{
    public class KullaniciService : IKullaniciService
    {
        private readonly IRepository<Kullanici> _kullaniciRepository;

        public KullaniciService(IRepository<Kullanici> kullaniciRepository)
        {
            _kullaniciRepository = kullaniciRepository;
        }

        public async Task<Kullanici> GetKullaniciBilgileriAsync(int id)
        {
            // İş kuralları buraya yazılabilir (Örn: null ise hata fırlat vb.)
            return await _kullaniciRepository.GetAsync(k => k.Id == id);
        }
    }
}