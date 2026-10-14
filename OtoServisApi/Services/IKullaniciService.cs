using OtoServis.Data.Models;

namespace OtoServisApi.Services
{
    public interface IKullaniciService
    {
        Task<Kullanici> GetKullaniciBilgileriAsync(int id);
    }
}
