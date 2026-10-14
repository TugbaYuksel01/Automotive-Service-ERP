using OtoServisApi.DTOs;

namespace OtoServisApi.Services
{
    public interface IAuthService
    {
        Task<string> RegisterAsync(RegisterDto dto);
        Task<string> LoginAsync(LoginDto dto);
        Task<int> MisafirKaydetVeyaBulAsync(string adSoyad, string email, string telefon);
        Task<(bool BasariliMi, string Mesaj)> SifreSifirlamaKoduGonderAsync(SifremiUnuttumDto dto);
        Task<(bool BasariliMi, string Mesaj)> YeniSifreyiKaydetAsync(SifreSifirlaDto dto);
        Task<(bool BasariliMi, string Mesaj)> PersonelEkleAsync(PersonelEkleDto dto, string atanacakRol);

        Task<OtoServis.Data.Models.Kullanici> GetProfilBilgileriAsync(int kullaniciId);
        Task<(bool BasariliMi, string Mesaj)> ProfilGuncelleAsync(int kullaniciId, ProfilGuncelleDto dto);

        Task<(bool BasariliMi, string Mesaj)> SifreGuncelleAsync(int kullaniciId, SifreGuncelleDto dto);
    }
}
