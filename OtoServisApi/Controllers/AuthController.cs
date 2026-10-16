using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Distributed;
using OtoServisApi.DTOs;
using OtoServisApi.Services;
using System.IdentityModel.Tokens.Jwt;

namespace OtoServisApi.Controllers
{
    [EnableRateLimiting("SabitZamanliKural")]
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IKullaniciService _kullaniciService;


        public AuthController(IAuthService authService, IKullaniciService kullaniciService)
        {
            _authService = authService; // <-- HATAYI ÇÖZECEK OLAN EKSİK SATIR
            _kullaniciService = kullaniciService;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            try
            {
                var sonuc = await _authService.RegisterAsync(dto);
                return Ok(new { Mesaj = sonuc });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Hata = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                var token = await _authService.LoginAsync(dto);
                return Ok(new { Token = token });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Hata = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromServices] IDistributedCache cache)
        {
            // 1. İstekten gelen ham Bearer token'ı al
            var rawToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "").Trim();
            if (string.IsNullOrEmpty(rawToken))
                return BadRequest("Token bulunamadı.");

            // 2. Token'ın süresinin bitmesine kaç dakika/saat kaldığını hesapla
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(rawToken);
            var kalanSure = jwtToken.ValidTo - DateTime.UtcNow;

            // Eğer süre zaten dolmuşsa işlem yapmaya gerek yok
            if (kalanSure <= TimeSpan.Zero)
                return Ok(new { Mesaj = "Oturum zaten sonlanmış." });

            // 3. Token'ı kalan ömrü kadar Redis'e 'kara liste' olarak kaydet
            var cacheKey = $"blacklist:{rawToken}";
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = kalanSure
            };

            await cache.SetStringAsync(cacheKey, "revoked", cacheOptions);

            return Ok(new { Mesaj = "Başarıyla çıkış yapıldı. Token geçersiz kılındı." });
        }

        [AllowAnonymous]
        [HttpPost("sifremi-unuttum")]
        public async Task<IActionResult> SifremiUnuttum([FromBody] SifremiUnuttumDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var sonuc = await _authService.SifreSifirlamaKoduGonderAsync(dto);
            return Ok(new { Mesaj = sonuc.Mesaj });
        }

        [AllowAnonymous]
        [HttpPost("sifre-sifirla")]
        public async Task<IActionResult> SifreSifirla([FromBody] SifreSifirlaDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var sonuc = await _authService.YeniSifreyiKaydetAsync(dto);
            if (!sonuc.BasariliMi)
            {
                return BadRequest(new { Mesaj = sonuc.Mesaj });
            }

            return Ok(new { Mesaj = sonuc.Mesaj });
        }
                          


        //[Authorize] // Sadece giriş yapmış olan kullanıcılar güncelleyebilir
        //[HttpPut("profil-guncelle")]
        //public async Task<IActionResult> ProfilGuncelle([FromBody] ProfilGuncelleDto dto)
        //{
        //    if (!ModelState.IsValid) return BadRequest(ModelState);

        //    // İstek atan kullanıcının ID'sini JWT Token içinden güvenli bir şekilde çekiyoruz
        //    var kullaniciIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        //    if (kullaniciIdClaim == null) return Unauthorized();

        //    int kullaniciId = int.Parse(kullaniciIdClaim.Value);

        //    var sonuc = await _authService.ProfilGuncelleAsync(kullaniciId, dto);
        //    if (!sonuc.BasariliMi)
        //    {
        //        return BadRequest(new { Mesaj = sonuc.Mesaj });
        //    }

        //    return Ok(new { Mesaj = sonuc.Mesaj });
        //}


        [Authorize]
        [HttpPut("sifre-guncelle")]
        public async Task<IActionResult> SifreGuncelle([FromBody] SifreGuncelleDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var kullaniciIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (kullaniciIdClaim == null) return Unauthorized();

            var sonuc = await _authService.SifreGuncelleAsync(int.Parse(kullaniciIdClaim.Value), dto);

            if (!sonuc.BasariliMi)
                return BadRequest(new { Mesaj = sonuc.Mesaj });

            return Ok(new { Mesaj = sonuc.Mesaj });
        }

        [Authorize]
        [HttpGet("profil")]
        public async Task<IActionResult> GetProfil()
        {
            // 1. Token'dan kullanıcının ID'sini çekiyoruz
            var kullaniciIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (kullaniciIdClaim == null) return Unauthorized();

            int kullaniciId = int.Parse(kullaniciIdClaim.Value);

            // 2. Hazır olan servisi kullanıp veritabanından müşteriyi buluyoruz
            var kullanici = await _kullaniciService.GetKullaniciBilgileriAsync(kullaniciId);

            if (kullanici == null) return NotFound("Kullanıcı bulunamadı.");

            // 3. Şifre gibi hassas verileri gizleyerek sadece gerekenleri Frontend'e yolluyoruz
            return Ok(new
            {
                adSoyad = kullanici.AdSoyad,
                email = kullanici.Email,
                telefon = kullanici.Telefon
            });
        }



    }
}