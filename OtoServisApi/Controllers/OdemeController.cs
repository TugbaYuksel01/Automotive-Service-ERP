using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OtoServisApi.DTOs;
using OtoServis.Data.Models;
using OtoServis.Data.Repositories;
using System.Security.Claims;

namespace OtoServisApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Token'ı olan (müşteri) ödeme yapabilir
    public class OdemeController : ControllerBase
    {
        private readonly IRepository<Fatura> _faturaRepository;
        private readonly IRepository<Randevu> _randevuRepository;


        public OdemeController(IRepository<Fatura> faturaRepository, IRepository<Randevu> randevuRepository)
        {
            _faturaRepository = faturaRepository;
            _randevuRepository = randevuRepository;
        }


        [Authorize(Roles = "Veznedar,SuperAdmin")]
        [HttpPost("randevu/{randevuId}/fiziksel-ode")]
        public async Task<IActionResult> FizikselOdemeAlByRandevu(int randevuId, [FromQuery] string odemeTuru)
        {
            // 1. Önce faturanın var olup olmadığını kontrol ediyoruz
            var fatura = await _faturaRepository.GetAsync(f => f.RandevuId == randevuId);
            if (fatura == null)
            {
                return NotFound($"#{randevuId} numaralı randevuya ait kesilmiş bir fatura sistemde bulunamadı.");
            }

            // 2. Fatura var ama daha önce ödenmiş mi?
            if (fatura.OdendiMi)
            {
                return BadRequest("Bu fatura zaten daha önce ödenmiş ve kapatılmıştır.");
            }

            fatura.OdendiMi = true;
            fatura.OdemeTarihi = DateTime.Now;
            fatura.OdemeYontemi = odemeTuru;
            fatura.TahsilEdenKullaniciId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            await _faturaRepository.UpdateAsync(fatura);
            await _faturaRepository.SaveChangesAsync();

            return Ok(new
            {
                Mesaj = $"#{randevuId} nolu randevunun ödemesi veznede ({odemeTuru}) olarak tahsil edildi.",
                FaturaId = fatura.Id,
                RandevuId = randevuId
            });
        }

        [Authorize(Roles = "Veznedar,SuperAdmin")]
        [HttpPut("{id}/teslim-et")]
        public async Task<IActionResult> AraciTeslimEt(int id)
        {
            var randevu = await _randevuRepository.GetAsync(r => r.Id == id);
            if (randevu == null) return NotFound("Randevu bulunamadı.");

            if (randevu.Durum != RandevuDurumu.Tamamlandi)
            {
                return BadRequest("Usta henüz bakım/onarım işlemlerini tamamlamadı.");
            }

            var fatura = await _faturaRepository.GetAsync(f => f.RandevuId == id);
            if (fatura == null || !fatura.OdendiMi)
            {
                return BadRequest("Ödeme onayı bulunamadı! Fatura tahsil edilmeden araç teslim edilemez.");
            }

            randevu.TeslimTarihi = DateTime.Now;
            randevu.TeslimEdenKullaniciId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            randevu.Durum = RandevuDurumu.Tamamlandi; // veya RandevuDurumu.TeslimEdildi
            await _randevuRepository.SaveChangesAsync();
            return Ok(new
            {

                Mesaj = "Ödeme kontrolü tamamlandı. Araç ve anahtar müşteriye teslim edilebilir.",
                RandevuId = id
            });
        }
    }
}
