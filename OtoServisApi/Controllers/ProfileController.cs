using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OtoServis.Data.Models;
using OtoServis.Data.Repositories;
using System.Security.Claims;
using OtoServisApi.DTOs;

namespace OtoServisApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // KURAL: Token'ı olan herkes (Müşteri, Usta, Admin) girebilir
    public class ProfilController : ControllerBase
    {
        private readonly IRepository<Kullanici> _kullaniciRepository;

        public ProfilController(IRepository<Kullanici> kullaniciRepository)
        {
            _kullaniciRepository = kullaniciRepository;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ProfilimiGetir()
        {
            // Token'dan giriş yapan kişinin ID'sini güvenli şekilde alıyoruz
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var kullanici = await _kullaniciRepository.GetAsync(k => k.Id == userId);
            if (kullanici == null) return NotFound("Kullanıcı bulunamadı.");

            // Şifre Hash'ini frontende GÖNDERMİYORUZ (Güvenlik)
            return Ok(new
            {
                kullanici.AdSoyad,
                kullanici.Email,
                kullanici.Telefon,
                kullanici.Rol
            });
        }

        [Authorize]
        [HttpPut("guncelle")]
        public async Task<IActionResult> ProfilimiGuncelle([FromBody] ProfilGuncelleDto dto)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var kullanici = await _kullaniciRepository.GetAsync(k => k.Id == userId);

            if (kullanici == null) return NotFound("Kullanıcı bulunamadı.");

            // Bilgileri Güncelle
            kullanici.AdSoyad = dto.AdSoyad;
            kullanici.Telefon = dto.Telefon;
            kullanici.Email = dto.Email;

            // Eğer kullanıcı yeni şifre girdiyse (boş bırakmadıysa) şifreyi de hashleyip güncelle
            if (!string.IsNullOrWhiteSpace(dto.YeniSifre))
            {
                kullanici.SifreHash = BCrypt.Net.BCrypt.HashPassword(dto.YeniSifre);
            }

            await _kullaniciRepository.UpdateAsync(kullanici);
            await _kullaniciRepository.SaveChangesAsync();

            // MİMARİ NOT: Eğer Usta'nın AdSoyad'ı değişirse, "Ustalar" tablosundaki AdSoyad da değişmeli!
            // Gerçek bir kurumsal sistemde bunu yapmak için burada Usta tablosuna da update atabiliriz.

            return Ok(new { Mesaj = "Profil bilgileriniz başarıyla güncellendi." });
        }
    }

    
}
