using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Distributed;
using OtoServis.Data.Models;
using OtoServis.Data.Repositories;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OtoServisApi.Controllers
{
    [EnableRateLimiting("SabitZamanliKural")]
    [Route("api/[controller]")]
    [ApiController]
    // Sadece SuperAdmin, Admin ve yeni ekleyeceğimiz DepoSorumlusu girebilir!
    [Authorize(Roles = "SuperAdmin,Admin,DepoSorumlusu,Usta")]
    public class YedekParcalarController : ControllerBase
    {
        private readonly IRepository<YedekParca> _yedekParcaRepository;
        private readonly IDistributedCache _cache;

        public YedekParcalarController(IRepository<YedekParca> yedekParcaRepository, IDistributedCache cache)
        {
            _yedekParcaRepository = yedekParcaRepository;
            _cache = cache;
        }

        [Authorize(Roles = "SuperAdmin, Admin, DepoSorumlusu, Usta")]
        [HttpGet("tum-stoklar")]
        public async Task<IActionResult> GetYedekParcalar()
        {
            string cacheKey = "tum_yedek_parcalar";

            // 1. Önce Redis'e bak (Cache Hit)
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                var jsonParcalar = JsonSerializer.Deserialize<List<YedekParca>>(cachedData);
                return Ok(jsonParcalar);
            }

            // 2. Redis'te yoksa veritabanından çek (Cache Miss)
            var parcalar = (await _yedekParcaRepository.GetAllAsync()).ToList();

            // 3. Veriyi 60 dakika geçerli olacak şekilde Redis'e yaz
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60)
            };
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(parcalar), cacheOptions);

            return Ok(parcalar);
        }

        [Authorize(Roles = "SuperAdmin, Admin, DepoSorumlusu, Usta")]
        [HttpGet("kritik-stoklar")]
        public async Task<IActionResult> KritikStoklariGetir()
        {
            // Stok miktarı, kritik seviyenin altına düşenleri bul (Ön yüzdeki kırmızı uyarı ekranı için)
            var kritikParcalar = await _yedekParcaRepository.GetAllAsync(p => p.StokMiktari <= p.KritikStokSeviyesi);

            if (!kritikParcalar.Any()) return Ok(new { Mesaj = "Harika! Kritik seviyede hiçbir parça yok." });

            return Ok(kritikParcalar);
        }

        //YENİ PARÇA EKLENDİĞİNDE
        [HttpPost]
        [Authorize(Roles = "SuperAdmin,Admin,DepoSorumlusu")]
        public async Task<ActionResult<YedekParca>> PostYedekParca(YedekParca yedekParca, [FromServices] IDistributedCache cache)
        {
            await _yedekParcaRepository.AddAsync(yedekParca);
            await _yedekParcaRepository.SaveChangesAsync();

            // Önbelleği temizle
            await cache.RemoveAsync("tum_yedek_parcalar");

            return CreatedAtAction(nameof(GetYedekParcalar), new { id = yedekParca.Id }, yedekParca);
        }

        //PARÇA/ STOK GÜNCELLENDİĞİNDE
        [HttpPut("{id}")]
        [Authorize(Roles = "SuperAdmin,Admin,DepoSorumlusu")]
        public async Task<IActionResult> PutYedekParca(int id, YedekParca yedekParca, [FromServices] IDistributedCache cache)
        {
            if (id != yedekParca.Id) return BadRequest("ID uyuşmazlığı.");

            await _yedekParcaRepository.UpdateAsync(yedekParca);
            await _yedekParcaRepository.SaveChangesAsync();

            // Güncel stok/fiyat bilgisinin yansıması için önbelleği temizle
            await cache.RemoveAsync("tum_yedek_parcalar");

            return NoContent();
        }

        //PARÇA SİLİNDİĞİNDE
        [HttpDelete("{id}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> DeleteYedekParca(int id, [FromServices] IDistributedCache cache)
        {
            var parca = await _yedekParcaRepository.GetByIdAsync(id);
            if (parca == null) return NotFound();

            await _yedekParcaRepository.DeleteAsync(parca);
            await _yedekParcaRepository.SaveChangesAsync();

            // Silinen parça listeden düşsün diye önbelleği temizle
            await cache.RemoveAsync("tum_yedek_parcalar");

            return NoContent();
        }
    }
}