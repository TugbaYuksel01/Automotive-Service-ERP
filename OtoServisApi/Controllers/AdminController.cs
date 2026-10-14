using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OtoServisApi.DTOs;
using OtoServisApi.Services;

namespace OtoServisApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AdminController(IAuthService authService)
        {
            _authService = authService;
        }

        // --- SADECE SUPERADMIN KULLANABİLİR ---
        [HttpPost("admin-ekle")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> AdminEkle([FromBody] PersonelEkleDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Güvenlik: DTO içinden gelen Rol verisini görmezden geliyoruz. 
            // SuperAdmin bu ucu kullandığı an eklenecek kişi zorunlu "Admin" olur.
            var sonuc = await _authService.PersonelEkleAsync(dto, "Admin");

            if (!sonuc.BasariliMi) return BadRequest(new { Mesaj = sonuc.Mesaj });

            return Ok(new { Mesaj = "Yeni Admin başarıyla eklendi." });
        }

        // --- ADMIN VE SUPERADMIN KULLANABİLİR ---
        [HttpPost("personel-ekle")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> PersonelEkle([FromBody] PersonelEkleDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Güvenlik Kalkanı: Admin, kendi kendine SuperAdmin veya başka bir Admin ekleyemez!
            var yasakliRoller = new List<string> { "SuperAdmin", "Admin" };

            if (yasakliRoller.Any(r => r.Equals(dto.Rol, StringComparison.OrdinalIgnoreCase))) // büyük küçük harf duyarlılığını ortadan kaldırıyoruzz
            if (yasakliRoller.Contains(dto.Rol))
            {
                return BadRequest(new { Mesaj = "Bu yetki seviyesinde bir personel ekleme izniniz yok." });
            }

            // Güvenlikten geçtiyse, kullanıcının belirlediği rolü ("Usta", "Vezne" vb.) servise yolla
            var sonuc = await _authService.PersonelEkleAsync(dto, dto.Rol);
            

            if (!sonuc.BasariliMi) return BadRequest(new { Mesaj = sonuc.Mesaj });

            return Ok(new { Mesaj = $"{dto.Rol} rolündeki personel başarıyla eklendi." });
        }
    }
}
