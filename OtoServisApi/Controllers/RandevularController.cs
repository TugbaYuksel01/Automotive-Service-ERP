using MassTransit; // RabbitMQ için gerekli
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using OtoServerApi.Services;
using OtoServis.Data.Models;
using OtoServis.Data.Repositories;  
using OtoServisApi.DTOs;
using OtoServisApi.Services;
using System.Security.Claims; // Token içinden veri okumak için gerekli




namespace OtoServisApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // 1. KURAL: Bu sınıftaki HİÇBİR uç noktaya yaka kartı (Token) olmadan ulaşılamaz.
    public class RandevularController : ControllerBase
    {
        private readonly IRandevuService _randevuService;
        private readonly IKullaniciService _kullaniciService;
        private readonly IRepository<Arac> _aracRepository;
        private readonly IRepository<Randevu> _randevuRepository;
        private readonly IAuthService _authService;
        private readonly IPublishEndpoint _publishEndpoint; // RabbitMQ için gerekli
        private readonly IRepository<Kullanici> _kullaniciRepository;
        private readonly IRepository<Usta> _ustaRepository;
        private readonly IAiTriageService _aiTriageService;

        public RandevularController(
            IRandevuService randevuService,
            IKullaniciService kullaniciService,
            IRepository<Arac> aracRepository,
            IRepository<Randevu> randevuRepository,
            IAuthService authService,
            IPublishEndpoint publishEndpoint,
            IRepository<Kullanici> kullaniciRepository,
            IRepository<Usta> ustaRepository,
             IAiTriageService aiTriageService
            )
        {
            _randevuService = randevuService;
   
            _kullaniciService = kullaniciService;
            _aracRepository = aracRepository;
            _randevuRepository = randevuRepository;
            _authService = authService;
            _publishEndpoint = publishEndpoint;
            _kullaniciRepository = kullaniciRepository;
            _ustaRepository = ustaRepository;
            _aiTriageService = aiTriageService;        }

            [HttpPost("Kayitli Randevu")]
        public async Task<IActionResult> KayitliYeniRandevu([FromBody] KayitliRandevuTalebiDto kisaDto)
        {
            try
            {
                var musteriId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
                var guncelKullanici = await _kullaniciService.GetKullaniciBilgileriAsync(musteriId);

                if (kisaDto.Tarih.ToLocalTime() < DateTime.Now)
                    return BadRequest("Geçmiş bir tarihe veya saate randevu alamazsınız.");

                string sonMarkaModel = kisaDto.AracMarkaModel;
                int sonUretimYili = kisaDto.AracUretimYili ?? 0;

                if (string.IsNullOrEmpty(kisaDto.AracMarkaModel))
                {
                    var kayitliAracList = await _aracRepository.GetAllAsync(a => a.Plaka == kisaDto.AracPlaka);
                    var KayitliArac = kayitliAracList.FirstOrDefault();
                    if (KayitliArac != null)
                    {
                        sonMarkaModel = KayitliArac.MarkaModel;
                        sonUretimYili = KayitliArac.UretimYili;
                    }
                }

                // =====================================================================
                // 2. YENİ: SENKRON YAPAY ZEKA ANALİZİ
                // =====================================================================
                var aiSonucu = await _aiTriageService.SikayetiAnalizEtAsync(kisaDto.MusteriNotu, kisaDto.AracPlaka);

                // Ortak DTO'yu oluştururken HizmetTipiId'yi AI'dan alıyoruz
                var anaDto = new RandevuOlusturDto
                {
                    MusteriId = guncelKullanici.Id,
                    MusteriAdSoyad = guncelKullanici.AdSoyad,
                    MusteriTelefon = guncelKullanici.Telefon ?? "Belirtilmemiş",
                    AracPlaka = kisaDto.AracPlaka,
                    AracMarkaModel = sonMarkaModel,
                    AracUretimYili = sonUretimYili,
                    Tarih = kisaDto.Tarih,
                    MusteriNotu = kisaDto.MusteriNotu,
                    HizmetTipiId = aiSonucu.OnerilenHizmetId, // GERÇEK HİZMET!
                    Email = guncelKullanici.Email
                };

                // 3. KAPASİTE KONTROLÜ (Bu metot çalışırken DTO'nun içindeki AcilMi değerini gizlice dolduracak)
                await _randevuService.AciliyetVeKapasiteKontroluAsync(anaDto);

                // 4. RANDEVU OLUŞTUR (Metot artık sadece db kayıt işlemlerine odaklanacak)
                int randevuID = await _randevuService.RandevuOlusturAsync(anaDto);
                if (randevuID < 0) return BadRequest("Randevu oluşturulamadı.");

                return Ok(new
                {
                    Mesaj = $"Yapay Zeka Teşhisi: {aiSonucu.YapayZekaAciklamasi}\n\nİşleminiz atölyemize başarıyla iletilmiştir.",
                    RandevuId = randevuID
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Mesaj = ex.Message });
            }
        }






        [AllowAnonymous]
        [HttpPost("Misafir Randevu")]
        public async Task<IActionResult> MisafirYeniRandevu(RandevuOlusturDto dto)
        {
            try
            {
                // 1. Müşteri Kaydı ve Tarih Kontrolü
                int musteriId = await _authService.MisafirKaydetVeyaBulAsync(dto.MusteriAdSoyad, dto.Email, dto.MusteriTelefon);
                if (dto.Tarih.ToLocalTime() < DateTime.Now)
                    return BadRequest("Geçmiş bir tarihe veya saate randevu alamazsınız.");

                // =====================================================================
                // 2. YENİ: SENKRON YAPAY ZEKA ANALİZİ (Önce Teşhis, Sonra Kapasite!)
                // =====================================================================
                var aiSonucu = await _aiTriageService.SikayetiAnalizEtAsync(dto.MusteriNotu, dto.AracPlaka);

                

                // Yapay zekanın bulduğu GERÇEK hizmet ID'sini (Örn: Ağır Bakım = 4) DTO'ya basıyoruz.
                dto.HizmetTipiId = aiSonucu.OnerilenHizmetId;

                // 3. KAPASİTE KONTROLÜ (Bu metot çalışırken DTO'nun içindeki AcilMi değerini gizlice dolduracak)
                await _randevuService.AciliyetVeKapasiteKontroluAsync(dto);


                // 4. RANDEVU OLUŞTUR (Metot artık sadece db kayıt işlemlerine odaklanacak)
                int randevuID = await _randevuService.RandevuOlusturAsync(dto);
                if (randevuID < 0) return BadRequest("Randevu oluşturulamadı.");

                // 5. ANINDA, KESİN CEVAP DÖN! (Arka plana event atmaya gerek kalmadı)
                return Ok(new
                {
                    Mesaj = $"Yapay Zeka Teşhisi: {aiSonucu.YapayZekaAciklamasi}\n\nİşleminiz atölyemize başarıyla iletilmiştir.",
                    RandevuId = randevuID
                });
            }
            catch (Exception ex)
            {
                // Kapasite doluysa, metottan fırlayan akıllı öneri (Şu saate alın vs.) direkt buraya düşer
                return BadRequest(new { Mesaj = ex.Message });
            }
        }


        [Authorize(Roles = "Musteri")]
        [HttpGet("benim-randevularim")]
        public async Task<IActionResult> BenimRandevularimiGetir()
        {
            // 1. Kimliği Token'dan Çek
            var kullaniciIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (kullaniciIdClaim == null) return Unauthorized();

            int kullaniciId = int.Parse(kullaniciIdClaim.Value);

            // 2. İşi Service'e Devret
            var randevular = await _randevuService.GetMusteriRandevulariAsync(kullaniciId);

            // 3. Sonucu Dön
            if (randevular == null || !randevular.Any())
            {
                return Ok(new { Mesaj = "Henüz alınmış bir randevunuz bulunmamaktadır." });
            }

            return Ok(randevular);
        }




        // DELETE: api/Randevular/5
        [Authorize(Roles = "SuperAdmin, Admin, Musteri")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRandevu(int id)
        {
            var basariliMi = await _randevuService.RandevuIptalEtAsync(id);

            if (!basariliMi)
            {
                return BadRequest("Randevu iptal edilemedi. Randevu bulunamamış veya randevuya 2 saatten az kalmış olabilir.");
            }

            return Ok("Randevunuz başarıyla iptal edildi.");
        }

        
        // PUT: api/Randevular/5/sure-uzat
        [Authorize(Roles = "SuperAdmin, Admin, Usta")]
        [HttpPut("{id}/sure-uzat")]
        public async Task<IActionResult> SureUzat(int id, [FromBody] SureUzatDto dto)
        {
            var basariliMi = await _randevuService.SureUzatVeArkadakileriKaydirAsync(id, dto);

            if (!basariliMi)
            {
                return NotFound("İşlem başarısız. Randevu bulunamadı.");
            }

            return Ok("Süre başarıyla uzatıldı. Sistem olası krizleri otonom olarak çözdü (Plan A / Plan B uygulandı).");
        }

        [Authorize(Roles = "SuperAdmin, Admin, Usta")]
        [HttpPut("{id}/basla")]
        public async Task<IActionResult> RandevuBaslat(int id)
        {
            var basariliMi = await _randevuService.RandevuBaslatAsync(id);

            if (!basariliMi)
            {
                return BadRequest("İşlem başlatılamadı. Randevu bulunamadı veya zaten işlemde/tamamlanmış.");
            }

            return Ok("Randevu işleme alındı. Usta ve istasyon için gerçek zamanlı takip başladı.");
        }

        [AllowAnonymous]
        [HttpGet("sorgula")]
        public async Task<IActionResult> DurumSorgula([FromQuery] int id, [FromQuery] string plaka)
        {
            var randevular = await _randevuRepository.GetAllAsync(
                    r => r.Id == id && r.Arac.Plaka == plaka, // 1. Parametre: Filtremiz (Where)
                    r => r.Arac,                              // 2. Parametre: Include 1
                    r => r.HizmetTipi,                        // 3. Parametre: Include 2
                    r => r.Fatura                             // 4. Parametre: Include 3
                );
            var randevu = randevular.FirstOrDefault();
            if (randevu == null) return NotFound(new { Mesaj = "Randevu bulunamadı veya plaka eşleşmedi." });

            return Ok(new
            {
                Durum = randevu.Durum,
                AracPlaka = randevu.Arac.Plaka,
                Tarih = randevu.Tarih,
                HizmetAdi = randevu.HizmetTipi.Ad,
                ToplamTutar = randevu.Fatura?.GenelToplam ?? 0
            });
        }




        [Authorize(Roles = "SuperAdmin, Admin, Usta")]
        [HttpPost("tamamla")]
        public async Task<IActionResult> RandevuTamamla([FromBody] RandevuTamamlamaDto dto)
        {
            try
            {
                var fatura = await _randevuService.IslemiTamamlaVeFaturaKesAsync(dto);
                return Ok(new { Mesaj = "İşlem bitti, fatura kesildi.", FaturaTutari = fatura.GenelToplam });
            }
            catch (Exception ex)
            {
                // ex.ToString() tüm InnerException'ları ve satır numaralarını JSON olarak döner
                return BadRequest(new { DetayliHata = ex.ToString() });
            }
        }

        
        [Authorize(Roles = "Usta")]
        [HttpGet("ustanin-gunluk-randevulari")]
        public async Task<IActionResult> GetUstaninGunlukRandevulari()
        {
            // 1. Token'dan giriş yapan kullanıcının ID'sini yakalıyoruz
            var kullaniciIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (kullaniciIdClaim == null) return Unauthorized();

            int kullaniciId = int.Parse(kullaniciIdClaim.Value);

            // 2. Servisten Usta'nın randevularını çekiyoruz (İçinde devasa döngüler olan ham veri)
            var randevular = await _randevuService.GetUstaninGunlukRandevulariAsync(kullaniciId);

            // 3. CYCLE HATASINI KIRAN YER: Sadece Frontend'in ihtiyacı olan alanları anonim nesneye çeviriyoruz (Tıpkı Admin'de yaptığın gibi)
            var sonuc = randevular.Select(r => new {
                r.Id,
                r.Tarih,
                r.Durum,
                r.MusteriNotu,
                r.AcilMi,
                // İç içe giren devasa ilişkileri koparıp, sadece yüzeysel verileri alıyoruz:
                Arac = r.Arac != null ? new { r.Arac.Plaka, r.Arac.MarkaModel } : null,
                Kullanici = r.Kullanici != null ? new { r.Kullanici.AdSoyad, r.Kullanici.Telefon } : null,
                Usta = r.Usta != null ? new { r.Usta.AdSoyad } : null,
                HizmetTipi = r.HizmetTipi != null ? new { r.HizmetTipi.Ad } : null
            });

            return Ok(sonuc);
        }







        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpGet("gunluk-tum-randevular")]
        public async Task<IActionResult> GetGunlukTumRandevular()
        {
            var randevular = await _randevuService.GetGunlukTumRandevularAsync();

            // Döngüyü koparmak için sadece gerekli alanları dönüyoruz
            var sonuc = randevular.Select(r => new {
                r.Id,
                r.Tarih,
                r.Durum,
                r.MusteriNotu,
                Arac = r.Arac != null ? new { r.Arac.Plaka, r.Arac.MarkaModel } : null,
                Kullanici = r.Kullanici != null ? new { r.Kullanici.AdSoyad, r.Kullanici.Telefon } : null,
                Usta = r.Usta != null ? new { r.Usta.AdSoyad } : null,
                HizmetTipi = r.HizmetTipi != null ? new { r.HizmetTipi.Ad } : null
            });

            return Ok(sonuc);
        }





    }
}