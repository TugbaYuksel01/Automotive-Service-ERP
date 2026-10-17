using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Distributed;
using OtoServis.Data.Models;
using OtoServis.Data.Repositories;
using System.Text.Json;

namespace OtoServisApi.Controllers
{
    
    [Route("api/[controller]")]
    [ApiController]
    public class HizmetTipleriController : ControllerBase
    {
        private readonly IRepository<HizmetTipi> _hizmetTipiRepository;
        private readonly IDistributedCache _cache;

        public HizmetTipleriController(IRepository<HizmetTipi> hizmetTipiRepository, IDistributedCache cache)
         
        {
            _hizmetTipiRepository = hizmetTipiRepository;
            _cache = cache;
        }

        [HttpGet]
        [AllowAnonymous]
        
        public async Task<ActionResult<IEnumerable<HizmetTipi>>> GetHizmetTipleri()
        {
            string cacheKey = "tum_hizmetler";

            // 1. ADIM: Redis'e bağlanmayı DENE
            try
            {
                var cachedData = await _cache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedData))
                {
                    var jsonHizmetler = JsonSerializer.Deserialize<List<HizmetTipi>>(cachedData);
                    return Ok(jsonHizmetler);
                }
            }
            catch (Exception)
            {
                // Eğer Redis sunucusu kapalıysa veya çöktüyse kod buraya düşer.
                // Hata fırlatmayı engelliyoruz ki sistem çökmesin, doğrudan SQL'e geçsin!
               
            }

            // 2. ADIM: Redis'te yoksa VEYA Redis kapalıysa SQL Server'dan getir
            var hizmetler = (await _hizmetTipiRepository.GetAllAsync()).ToList();

            // 3. ADIM: Yeni veriyi Redis'e yazmayı DENE (Yine hata fırlatmasını engelliyoruz)
            try
            {
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                };
                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(hizmetler), cacheOptions);
            }
            catch (Exception)
            {
                // Redis kapalıysa yazamaz, sorun değil. Kullanıcıya verisini dönmeye devam et.
            }

            return Ok(hizmetler);
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<HizmetTipi>> PostHizmetTipi(HizmetTipi hizmetTipi, [FromServices] IDistributedCache cache)
        {
            await _hizmetTipiRepository.AddAsync(hizmetTipi);
            await _hizmetTipiRepository.SaveChangesAsync();

            // ÖNBELLEĞİ BOŞALT: Yeni hizmet eklendiği için eski listeyi çöpe atıyoruz
            await cache.RemoveAsync("tum_hizmetler");

            return CreatedAtAction(nameof(GetHizmetTipleri), new { id = hizmetTipi.Id }, hizmetTipi);
        }
    }
}