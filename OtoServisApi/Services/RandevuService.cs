using MassTransit;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using OtoServis.Contracts.Events;
using OtoServis.Data.Contracts;
using OtoServis.Data.Models;
using OtoServis.Data.Repositories;
using OtoServisApi.DTOs;
using OtoServisApi.Services;
using static StackExchange.Redis.Role;



namespace OtoServisApi.Services
{
    public class RandevuService : IRandevuService
    {
        private readonly IRandevuRepository _randevuRepository;
        private readonly IRepository<Usta> _ustaRepository;
        
        private readonly IRepository<Kullanici> _kullaniciRepository;
        private readonly IRepository<Arac> _aracRepository;

        private readonly IRepository<HizmetTipi> _hizmetTipiRepository;
        private readonly IRepository<YedekParca> _yedekParcaRepository;
        private readonly IRepository<RandevuParca> _randevuParcaRepository;
        private readonly IRepository<Fatura> _faturaRepository;
        private readonly IRepository<Istasyon> _istasyonRepository;
       
        private readonly IPdfFaturaService _pdfFaturaService;
        private readonly IPublishEndpoint _publishEndpoint; // 1. MassTransit arayüzü

        private readonly ILogger<KaynakPlanlamaMotoru> _logger;






        public RandevuService(
            IRandevuRepository randevuRepository,
            IRepository<Usta> ustaRepository,
            
            IRepository<Kullanici> kullaniciRepository,
            IRepository<Arac> aracRepository,
            IRepository<HizmetTipi> hizmetRepository,
            IRepository<YedekParca> yedekParcaRepository,
            IRepository<RandevuParca> randevuParcaRepository,
            IRepository<Fatura> faturaRepository,
            IRepository<Istasyon> istasyonRepository,
            IPdfFaturaService pdfFaturaService,
            IPublishEndpoint publishEndpoint, ILogger<KaynakPlanlamaMotoru> logger
            )
        {
            _randevuRepository = randevuRepository;
            _ustaRepository = ustaRepository;
            
            _kullaniciRepository = kullaniciRepository;
            _aracRepository = aracRepository;
            _hizmetTipiRepository = hizmetRepository;
            _yedekParcaRepository = yedekParcaRepository;
            _randevuParcaRepository = randevuParcaRepository;
            _faturaRepository = faturaRepository;
            _pdfFaturaService = pdfFaturaService;
            _publishEndpoint = publishEndpoint;
            _istasyonRepository = istasyonRepository;
            _logger = logger;


        }


        public async Task<IEnumerable<RandevuDetayDto>> TumRandevulariGetirAsync()
        {
            var randevular = await _randevuRepository.TumRandevulariIliskileriyleGetirAsync();

            return randevular.Select(r => new RandevuDetayDto
            {

                RandevuNo = r.Id,
                Tarih = r.Tarih.ToString("dd.MM.yyyy HH:mm"),
                MusteriBilgisi = r.Kullanici != null ? r.Kullanici.AdSoyad + " - " + r.Kullanici.Telefon : "Bilinmiyor",
                AracPlaka = r.Arac != null ? r.Arac.Plaka : "Bilinmiyor",
                HizmetAdi = r.HizmetTipi != null ? r.HizmetTipi.Ad : "Bilinmiyor",
                ToplamTutar = r.HizmetTipi != null ? r.HizmetTipi.Fiyat : 0,
                SorumluUsta = r.Usta != null ? r.Usta.AdSoyad : "Henüz Usta Atanmadı"
            });
        }


        public async Task AciliyetVeKapasiteKontroluAsync(RandevuOlusturDto dto)
        {
            _logger.LogInformation("ACİLİYET TESPİTİNE BAŞLANDI");
            // 1. İLK ADIM: ACİLİYET TESPİTİ (DTO'nun içine mühürlüyoruz)
            dto.AcilMi = false;
            var not = dto.MusteriNotu ?? "";

            string[] anahtarKelimeler = { "çekici", "cekici", "kaza", "yolda", "acil", "acıl" };

            foreach (var kelime in anahtarKelimeler)
            {
                if (not.IndexOf(kelime, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    dto.AcilMi = true;
                    break;
                }
            }

            // Eğer müşteri ACİL ise, kapasite dolsa bile aşağıya inmeden direkt geçiş izni ver!
            if (dto.AcilMi) return;
            _logger.LogInformation("bu mesajı görüyosan return olmadık yola devam");

            _logger.LogInformation("SEN acil değilmişsina ma dolu zamanımıza mı denk geldin bakalım bacım");
            // 2. KAPASİTE VE DOLULUK KONTROLÜ (Sadece Normal Müşteriler İçin)
            var talepEdilenHizmet = await _hizmetTipiRepository.GetByIdAsync(dto.HizmetTipiId);
            if (talepEdilenHizmet == null) throw new Exception("Geçersiz hizmet seçimi.");

            int ustaSure = talepEdilenHizmet.UstaSureDakika > 0 ? talepEdilenHizmet.UstaSureDakika : 30;
            int liftSure = talepEdilenHizmet.LiftSureDakika > 0 ? talepEdilenHizmet.LiftSureDakika : 60;

            
            
            DateTime istenenBaslangic = dto.Tarih.ToUniversalTime().AddHours(3);
            // ===============================================================
            // 🚨 MESAİ SAATİ VE KAPANIŞA YAKINLIK KONTROLÜ
            // ===============================================================
            TimeSpan mesaiBaslangic = new TimeSpan(9, 0, 0);
            TimeSpan mesaiBitis = new TimeSpan(18, 0, 0);
            // 1. Kural: Randevu direkt mesai saatleri dışında mı? (Örn: 08:00 veya 19:00)
            if (istenenBaslangic.TimeOfDay < mesaiBaslangic || istenenBaslangic.TimeOfDay >= mesaiBitis)
            {
                throw new Exception("Çalışma saatlerimiz 09:00 - 18:00 arasındadır. Lütfen mesai saatleri içerisinde bir zaman seçiniz.");
            }

            // 2. Kural: Randevu mesai içinde ama işlemin bitmesi mesaiyi aşıyor mu? (Örn: Saat 17:30'a 1 saatlik iş istenmesi)
            int isinMaksimumSuresi = Math.Max(ustaSure, liftSure);
            if (istenenBaslangic.TimeOfDay.Add(TimeSpan.FromMinutes(isinMaksimumSuresi)) > mesaiBitis)
            {
                throw new Exception($"Talep ettiğiniz işlemin tahmini süresi {isinMaksimumSuresi} dakikadır. İşlem bitişi mesai kapanış saatimizi (18:00) aşacağı için lütfen daha erken bir saat seçiniz.");
            }
            // ===============================================================
            DateTime ustaBitis = istenenBaslangic.AddMinutes(ustaSure);
            DateTime liftBitis = istenenBaslangic.AddMinutes(liftSure);

            // Çakışma hesaplayıcı yerel fonksiyon
            bool IkiZamanAraligiKesisiyorMu(DateTime baslangic1, int sure1, DateTime baslangic2, int sure2)
            {
                
                return baslangic1 < baslangic2.AddMinutes(sure2) && baslangic2 < baslangic1.AddMinutes(sure1);
                
            }

            _logger.LogInformation("bacım çakışma var mıymış?");
            // Gelecek saatleri önerebilmek için sadece bugünü değil, önümüzdeki 7 günü RAM'e alıyoruz
            DateTime aramaMaksimumSiniri = istenenBaslangic.AddDays(7);
            var gelecektekiIsler = (await _randevuRepository.GetAllAsync(
                r => r.Tarih >= istenenBaslangic.Date && r.Tarih <= aramaMaksimumSiniri &&
                     (r.Durum == RandevuDurumu.Bekliyor || r.Durum == RandevuDurumu.Islemde),
                r => r.HizmetTipi
            )).ToList();

            _logger.LogInformation("gelll kk liste geçti m eline");
            var ustalar = await _ustaRepository.GetAllAsync();
            var istasyonlar = await _istasyonRepository.GetAllAsync();

            // Sadece istenen saatin (O Anın) Doluluk Kontrolü
            int dukkaninToplamUzmanUstaSayisi = ustalar.Count(u => u.UzmanlikAlani == talepEdilenHizmet.GerekenUzmanlik);
            _logger.LogInformation($"{dukkaninToplamUzmanUstaSayisi}");
            int dukkaninToplamLiftSayisi = istasyonlar.Count();
            _logger.LogInformation($"{dukkaninToplamLiftSayisi}");

            int oSaattekiCakisanUstaIsiSayisi = gelecektekiIsler.Count(r =>
                r.HizmetTipi?.GerekenUzmanlik == talepEdilenHizmet.GerekenUzmanlik &&
                IkiZamanAraligiKesisiyorMu(istenenBaslangic, ustaSure, r.GerceklesenBaslangic ?? r.Tarih, r.HizmetTipi?.UstaSureDakika ?? 30)
            );
            

            int oSaattekiCakisanLiftIsiSayisi = gelecektekiIsler.Count(r =>
                IkiZamanAraligiKesisiyorMu(istenenBaslangic, liftSure, r.GerceklesenBaslangic ?? r.Tarih, r.HizmetTipi?.LiftSureDakika ?? 60)
            );

            bool ustaDolu = oSaattekiCakisanUstaIsiSayisi >= dukkaninToplamUzmanUstaSayisi;
            _logger.LogInformation($"{ustaDolu}");
            bool liftDolu = oSaattekiCakisanLiftIsiSayisi >= dukkaninToplamLiftSayisi;
            _logger.LogInformation($"{liftDolu}");

            _logger.LogInformation("buraya ulaşabildin mi bari");
            // EĞER KAPASİTE DOLUYSA ALTERNATİF SAAT ARAMAYA BAŞLA
            if (ustaDolu || liftDolu)
            {
                _logger.LogInformation("bacım kapasitemiz doluymuş ama senin için bi alternatif bakıcaz");
                DateTime alternatifSaat = istenenBaslangic;
                DateTime? enErkenBosSaat = null;
               
                TimeSpan molaBaslangic = new TimeSpan(12, 0, 0);
                TimeSpan molaBitis = new TimeSpan(13, 0, 0);

                while (alternatifSaat < aramaMaksimumSiniri)
                {
                    alternatifSaat = alternatifSaat.AddMinutes(15);
                    var testBitis = alternatifSaat.AddMinutes(Math.Max(ustaSure, liftSure));

                    // Mola saatine denk geliyorsa molanın bitişine atla
                    if (alternatifSaat.TimeOfDay < molaBitis && testBitis.TimeOfDay > molaBaslangic)
                    {
                        alternatifSaat = alternatifSaat.Date.Add(molaBitis);
                    }

                    // Mesai saatini aşıyorsa ertesi sabah 09:00'a atla (Pazara gelirse Pazartesiye atla)
                    if (testBitis.TimeOfDay > mesaiBitis)
                    {
                        alternatifSaat = alternatifSaat.Date.AddDays(1).Add(mesaiBaslangic);
                        if (alternatifSaat.DayOfWeek == DayOfWeek.Sunday) alternatifSaat = alternatifSaat.AddDays(1);
                        continue;
                    }

                    var uygunUstaVarMi = ustalar.Any(u =>
                        u.UzmanlikAlani == talepEdilenHizmet.GerekenUzmanlik &&
                        !gelecektekiIsler.Any(d => d.UstaId == u.Id &&
                        IkiZamanAraligiKesisiyorMu(alternatifSaat, ustaSure, d.GerceklesenBaslangic ?? d.Tarih, d.HizmetTipi?.UstaSureDakika ?? 30))
                    );

                    var uygunLiftVarMi = istasyonlar.Any(i =>
                        i.MusaitMi &&
                        !gelecektekiIsler.Any(d => d.IstasyonId == i.Id &&
                        IkiZamanAraligiKesisiyorMu(alternatifSaat, liftSure, d.GerceklesenBaslangic ?? d.Tarih, d.HizmetTipi?.LiftSureDakika ?? 60))
                    );

                    // Hem usta hem lift boşsa saati bulduk demektir!
                    if (uygunUstaVarMi && uygunLiftVarMi)
                    {
                        _logger.LogInformation("bacım şanslısınn valla boş usta da lift de yakaladın ");
                        enErkenBosSaat = alternatifSaat;
                        break;
                    }
                }

                _logger.LogInformation("Bacım biz doluymuşuz şansına küss");
                // Bulunan saate göre müşteriye verilecek zenginleştirilmiş hata mesajını oluştur
                string sebep = ustaDolu ? $"'{talepEdilenHizmet.GerekenUzmanlik}' uzmanlarımız" : "araç bakım istasyonlarımız";
                string hataMesaji = $"Seçtiğiniz {istenenBaslangic:HH:mm} saatinde {sebep} tam kapasite doludur. ";
                _logger.LogInformation("dolyuz abi");

                if (enErkenBosSaat.HasValue)
                {
                    string gunIfadesi = enErkenBosSaat.Value.Date == istenenBaslangic.Date ? "bugün" : enErkenBosSaat.Value.ToString("dd.MM.yyyy");
                    hataMesaji += $"Size en erken {gunIfadesi} saat {enErkenBosSaat.Value:HH:mm} için yardımcı olabiliriz. Lütfen bu saat için tekrar randevu oluşturunuz.";
                    _logger.LogInformation(("şu saate al randevuyu salak"));
                }
                else
                {
                    hataMesaji += "Önümüzdeki günlerde tüm istasyonlarımız doludur, lütfen daha ileri bir tarih seçiniz.";
                }

                // Endpoint'in (Controller'ın) yakalayıp ekranda göstereceği akıllı hatayı fırlat
                throw new Exception(hataMesaji);
            }

            
            
        }


        // Örnek Randevu Oluşturma / Ekleme Mantığı
        public async Task<int> RandevuOlusturAsync(RandevuOlusturDto dto)
        {
           
           var kullanici = await _kullaniciRepository.GetAsync(m => m.Email == dto.Email);
            

            //amaaa eğer önce tüm verileri alayım Repostory üzerinden ardından gelen veriye C# tarafında bir filteleme yapayım 
            //dersen bu tüm kayıtların(mesela 50.000 kayıt)RAMe getirilmesi demek bu da RAMi şişir yani filtreleme işlemini de 
            //Repostorye atmalıyız ki filtreleme C#tarfaında değil de EFin dbye göndereciği sorgu üzerinde olsun filtre dbde yapılsı

            //var musteriler = await _musteriRepository.GetAllAsync();
            //var musteri = musteriler.FirstOrDefault(m => m.Telefon == talep.MusteriTelefon);

            //BU YUKARIDAKİ İKİ SATIRLIK KOD RAMİ ŞİŞİRİCEK EFEKTİF DEĞİL!!!

            if (kullanici == null)
            {
                kullanici = new Kullanici
                {
                    AdSoyad = dto.MusteriAdSoyad,
                    Telefon = dto.MusteriTelefon,
                    Email = dto.Email,
                };
                await _kullaniciRepository.AddAsync(kullanici);
            }

            // 2. Aracı Bul veya Oluştur
            var arac = await _aracRepository.GetAsync(a => a.Plaka == dto.AracPlaka);

            if (arac == null)
            {
                arac = new Arac
                {
                    Plaka = dto.AracPlaka,
                    MarkaModel = dto.AracMarkaModel,
                    UretimYili = dto.AracUretimYili,
                    Kullanici = kullanici // ID yerine doğrudan nesneyi veriyoruz, EF Core bağlayacak
                };
                await _aracRepository.AddAsync(arac);
            }
            else if (arac.Kullanici?.Telefon != dto.MusteriTelefon)
            {
                // İkinci el durumu: Araç sistemde var ama başkasına aitmiş. Yeni sahibine devret.
                arac.Kullanici = kullanici;
                _aracRepository.UpdateAsync(arac);
            }

            // 3. Randevuyu Oluştur ve Tüm Nesneleri Bağla
            var randevu = new Randevu
            {
                Kullanici = kullanici,
                Arac = arac,
                HizmetTipiId = dto.HizmetTipiId,
                Tarih = dto.Tarih.ToUniversalTime().AddHours(3),
                MusteriNotu = dto.MusteriNotu,
                Durum = 0,

                // HARİKA SADELEŞTİRME BURADA: Sadece DTO'daki damgayı al!
                AcilMi = dto.AcilMi
            };
                       
            // Veritabanına kaydet...
            await _randevuRepository.AddAsync(randevu);
            await _randevuRepository.SaveChangesAsync(); // Tüm değişiklikleri tek transaction ile kaydediyoruz

            await _publishEndpoint.Publish(new RandevuOlusturulduEvent
            {
                RandevuId = randevu.Id,
                MusteriEmail = kullanici.Email ?? dto.Email,
                MusteriAdSoyad = kullanici.AdSoyad,
                RandevuTarihi = randevu.Tarih,
                AracPlaka = arac.Plaka,
                // YENİ EKLENEN: Kullanıcının rolü "Misafir" ise true, değilse false gider
                IsMisafir = kullanici.Rol.Equals("Misafir", StringComparison.OrdinalIgnoreCase)
            });

            // Müşteriye bekleme yaptırmadan hemen randevu numarasını dönüyoruz
            return randevu.Id;
        }

        public async Task<bool> RandevuIptalEtAsync(int randevuId)
        {
            var randevu = await _randevuRepository.GetByIdAsync(randevuId);
            if (randevu == null)
            {
                return false;
            }

            // İş Kuralı 2: Randevuya 2 saatten az kaldıysa iptal edilemez (Son Dakika Engeli)
            if ((randevu.Tarih - DateTime.Now).TotalHours < 2)
            {
                return false;
            }

            // YENİ YAKLAŞIM: Veriyi silmek yerine Durumunu "İptal" (3) yapıyoruz
            randevu.Durum = RandevuDurumu.Iptal;

            // KRİTİK NOKTA: Usta ve İstasyonu (Lift) SERBEST BIRAKIYORUZ!
            // Böylece Kaynak Planlama Motoru burayı boş görüp arkadaki randevuları buraya kaydırabilecek.
            randevu.UstaId = null;
            randevu.IstasyonId = null;

            await _randevuRepository.UpdateAsync(randevu);

            // EKSİK OLAN VE DB'YE YANSIMASINI SAĞLAYACAK KOD
            await _randevuRepository.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<Randevu>> GetMusteriRandevulariAsync(int musteriId)
        {
            // 1. Parametre: Filtre (KullaniciId'si eşleşenler)
            // 2. ve 3. Parametre: Include edilecek tablolar (Arac ve HizmetTipi)
            var randevular = await _randevuRepository.GetAllAsync(
                r => r.KullaniciId == musteriId,
                r => r.Arac,
                r => r.HizmetTipi
            );

            return randevular;
        }


        public async Task<bool> RandevuTamamlaAsync(int randevuId)
        {
            var randevu = await _randevuRepository.GetByIdAsync(randevuId);

            // TamamlandiMi yerine yeni Durum enum'unu kontrol ediyoruz
            if (randevu == null || randevu.Durum == RandevuDurumu.Tamamlandi)
                return false;

            // Randevuyu tamamlandı olarak işaretle ve bitiş saatini damgala
            randevu.Durum = RandevuDurumu.Tamamlandi;
            randevu.GerceklesenBitis = DateTime.Now; // Ustanın işi bitirdiği gerçek an

            await _randevuRepository.UpdateAsync(randevu);
            return true;
        }

        public async Task<bool> SureUzatVeArkadakileriKaydirAsync(int randevuId, SureUzatDto dto)
        {
            var tumRandevular = (await _randevuRepository.GetAllAsync(
                null,
                 r => r.Kullanici,
                 r => r.HizmetTipi,
                 r => r.Arac // Eksik olan Include eklendi
             )).ToList();

            var ustalar = (await _ustaRepository.GetAllAsync()).ToList();

            var randevu = await _randevuRepository.GetByIdAsync(randevuId);
            if (randevu == null) return false;

            if (randevu.Durum == RandevuDurumu.Bekliyor)
            {
                randevu.Durum = RandevuDurumu.Islemde;
                randevu.GerceklesenBaslangic = DateTime.Now;
                await _randevuRepository.UpdateAsync(randevu);
            }

            var arkadakiRandevular = tumRandevular
                .Where(r => r.UstaId == randevu.UstaId && r.Tarih > randevu.Tarih && r.Durum == RandevuDurumu.Bekliyor)
                .OrderBy(r => r.Tarih)
                .ToList();

            TimeSpan mesaiBaslangic = new TimeSpan(9, 0, 0);
            TimeSpan mesaiBitis = new TimeSpan(18, 0, 0);
            TimeSpan molaBaslangic = new TimeSpan(12, 0, 0);
            TimeSpan molaBitis = new TimeSpan(13, 0, 0);

            foreach (var arkaRandevu in arkadakiRandevular)
            {
                int islemSuresi = arkaRandevu.HizmetTipi?.UstaSureDakika ?? 30;
                bool alternatifBulundu = false;
                bool mesaiAsimiOldu = false;

                var maxErtelemeSaati = arkaRandevu.Tarih.AddMinutes(dto.EkSureDakika);
                var orijinalSaat = arkaRandevu.Tarih;
                var denenenSaat = orijinalSaat;

                while (denenenSaat <= maxErtelemeSaati)
                {
                    var denenenBitisSaati = denenenSaat.AddMinutes(islemSuresi);

                    if (denenenSaat.TimeOfDay < molaBitis && denenenBitisSaati.TimeOfDay > molaBaslangic)
                    {
                        denenenSaat = denenenSaat.Date.Add(molaBitis);
                        continue;
                    }

                    if (denenenBitisSaati.TimeOfDay > mesaiBitis)
                    {
                        mesaiAsimiOldu = true;
                        break;
                    }

                    foreach (var usta in ustalar)
                    {
                        if (usta.Id == randevu.UstaId) continue;
                        if (usta.UzmanlikAlani != arkaRandevu.HizmetTipi?.GerekenUzmanlik) continue;

                        bool ustaninCakisanIsiVarMi = tumRandevular.Any(r =>
                            r.Id != arkaRandevu.Id &&
                            r.UstaId == usta.Id &&
                            (r.Durum == RandevuDurumu.Bekliyor || r.Durum == RandevuDurumu.Islemde) &&
                            (r.Tarih < denenenSaat.AddMinutes(islemSuresi) && r.Tarih.AddMinutes(islemSuresi) > denenenSaat)
                        );

                        if (!ustaninCakisanIsiVarMi)
                        {
                            arkaRandevu.UstaId = usta.Id;
                            arkaRandevu.Tarih = denenenSaat;

                            await _randevuRepository.UpdateAsync(arkaRandevu);
                            alternatifBulundu = true;

                            if (denenenSaat > orijinalSaat)
                            {
                                await _publishEndpoint.Publish(new RandevuKaydirildiEvent
                                {
                                    RandevuId = arkaRandevu.Id,
                                    MusteriEmail = arkaRandevu.Kullanici?.Email ?? "musteri@ornek.com", // Null koruması eklendi
                                    MusteriAdSoyad = arkaRandevu.Kullanici?.AdSoyad ?? "Sayın Müşterimiz",
                                    AracPlaka = arkaRandevu.Arac?.Plaka ?? string.Empty, // Null koruması eklendi
                                    EskiTarih = orijinalSaat,
                                    YeniTarih = denenenSaat,
                                    Tip = KaydirmaTipi.HafifGuncelleme,
                                    Aciklama = "Önceki işlemlerin uzamasından dolayı randevunuz güncellenmiştir."
                                });
                            }
                            break;
                        }
                    }

                    if (alternatifBulundu) break;
                    denenenSaat = denenenSaat.AddMinutes(5);
                }

                if (mesaiAsimiOldu)
                {
                    var ertesiGun = arkaRandevu.Tarih.Date.AddDays(1).Add(mesaiBaslangic);
                    if (ertesiGun.DayOfWeek == DayOfWeek.Sunday) ertesiGun = ertesiGun.AddDays(1);

                    arkaRandevu.Tarih = ertesiGun;
                    arkaRandevu.UstaId = randevu.UstaId;

                    await _randevuRepository.UpdateAsync(arkaRandevu);

                    await _publishEndpoint.Publish(new RandevuKaydirildiEvent
                    {
                        RandevuId = arkaRandevu.Id,
                        MusteriEmail = arkaRandevu.Kullanici?.Email ?? "musteri@ornek.com",
                        MusteriAdSoyad = arkaRandevu.Kullanici?.AdSoyad ?? "Sayın Müşterimiz",
                        AracPlaka = arkaRandevu.Arac?.Plaka ?? string.Empty,
                        EskiTarih = orijinalSaat,
                        YeniTarih = ertesiGun,
                        Tip = KaydirmaTipi.VipErtesiGun,
                        Aciklama = "İşlem atölye mesaimizi aştığı için randevunuz ertesi gün sabah 'VIP İlk Araç' statüsüne taşınmıştır."
                    });
                }
                else if (!alternatifBulundu)
                {
                    arkaRandevu.Tarih = orijinalSaat.AddMinutes(dto.EkSureDakika);

                    if (arkaRandevu.Tarih.TimeOfDay < molaBitis && arkaRandevu.Tarih.AddMinutes(islemSuresi).TimeOfDay > molaBaslangic)
                    {
                        arkaRandevu.Tarih = arkaRandevu.Tarih.Date.Add(molaBitis);
                    }

                    await _randevuRepository.UpdateAsync(arkaRandevu);

                    await _publishEndpoint.Publish(new RandevuKaydirildiEvent
                    {
                        RandevuId = arkaRandevu.Id,
                        MusteriEmail = arkaRandevu.Kullanici?.Email ?? "musteri@ornek.com",
                        MusteriAdSoyad = arkaRandevu.Kullanici?.AdSoyad ?? "Sayın Müşterimiz",
                        AracPlaka = arkaRandevu.Arac?.Plaka ?? string.Empty,
                        EskiTarih = orijinalSaat,
                        YeniTarih = arkaRandevu.Tarih,
                        Tip = KaydirmaTipi.KrizErtelemesi,
                        Aciklama = "Tüm birimlerimizdeki acil yoğunluk sebebiyle randevu saatiniz zorunlu olarak kaydırılmıştır."
                    });
                }
            }

            // Performans iyileştirmesi: Tüm güncellemeler bittikten sonra tek seferde veritabanına yazılır 
            // FİNAL: Hangi repodan çağırdığının önemi yok, o anki DbContext'i tetikler! ve Ramde tutulan tüm işlemler(update,delete,vs) tek bir transaction(pakette) artık dbde kaydedilir
            await _randevuRepository.SaveChangesAsync();

            return true;
        }

        public async Task<bool> RandevuBaslatAsync(int randevuId)
        {
            // 1. Randevuyu buluyoruz
            var randevu = await _randevuRepository.GetByIdAsync(randevuId);
            if (randevu == null || randevu.Durum != RandevuDurumu.Bekliyor)
                return false;

            // Randevuyu işlemde olarak işaretle ve gerçek başlama saatini damgala
            randevu.Durum = RandevuDurumu.Islemde;
            randevu.GerceklesenBaslangic = DateTime.Now;

            await _randevuRepository.UpdateAsync(randevu);

            // 2. Eğer randevuya atanmış bir istasyon varsa, veritabanından çekip meşgule alıyoruz
            if (randevu.IstasyonId.HasValue)
            {
                var istasyon = await _istasyonRepository.GetAsync(i => i.Id == randevu.IstasyonId.Value);
                if (istasyon != null)
                {
                    istasyon.MusaitMi = false; // Lifti meşgule (dolu) çekiyoruz
                    await _istasyonRepository.UpdateAsync(istasyon);
                }
            }

            // --- EN KRİTİK NOKTA: DEĞİŞİKLİKLERİ TEK SEFERDE SQL'E KOMPLE YAZIYORUZ ---
            await _randevuRepository.SaveChangesAsync();
            return true;
        }
        public async Task<Fatura> IslemiTamamlaVeFaturaKesAsync(RandevuTamamlamaDto dto)
        {
            var randevu = await _randevuRepository.GetAsync(r => r.Id == dto.RandevuId);
            if (randevu == null) throw new Exception("Randevu bulunamadı.");
            if (randevu.Durum == RandevuDurumu.Tamamlandi) throw new Exception("Bu randevu zaten tamamlanmış.");

            var hizmet = await _hizmetTipiRepository.GetAsync(h => h.Id == randevu.HizmetTipiId);
            decimal iscilikUcreti = hizmet.Fiyat;
            decimal toplamParcaMaliyeti = 0;

            var faturayaGidecekParcalar = new List<YedekParca>();

            foreach (var parcaDto in dto.KullanilanParcalar)
            {
                var stoktakiParca = await _yedekParcaRepository.GetAsync(y => y.Id == parcaDto.YedekParcaId);

                if (stoktakiParca == null) throw new Exception("Yedek parça bulunamadı.");
                if (stoktakiParca.StokMiktari < parcaDto.KullanilanMiktar) throw new Exception($"{stoktakiParca.ParcaAdi} için stok yetersiz.");

                stoktakiParca.StokMiktari -= parcaDto.KullanilanMiktar;
                await _yedekParcaRepository.UpdateAsync(stoktakiParca);

                toplamParcaMaliyeti += (stoktakiParca.BirimFiyat * parcaDto.KullanilanMiktar);

                var randevuParca = new RandevuParca
                {
                    RandevuId = randevu.Id,
                    YedekParcaId = stoktakiParca.Id,
                    KullanilanMiktar = parcaDto.KullanilanMiktar
                };
                await _randevuParcaRepository.AddAsync(randevuParca);
                await _randevuParcaRepository.SaveChangesAsync();

                faturayaGidecekParcalar.Add(stoktakiParca);
            }

            // --- 1. İŞLEM BİTTİĞİ İÇİN RANDEVUYU GÜNCELLİYORUZ ---
            randevu.Durum = RandevuDurumu.Tamamlandi;
            randevu.GerceklesenBitis = DateTime.Now;
            await _randevuRepository.UpdateAsync(randevu);

            // --- 2. KRİTİK DOKUNUŞ: LİFTİ (İSTASYONU) TEKRAR BOŞA (MÜSAİTE) ÇIKARIYORUZ! ---
            if (randevu.IstasyonId.HasValue)
            {
                var istasyon = await _istasyonRepository.GetByIdAsync(randevu.IstasyonId.Value);
                if (istasyon != null)
                {
                    istasyon.MusaitMi = true; // Lifti tekrar MÜSAİT (1 / true) yaptık!
                    await _istasyonRepository.UpdateAsync(istasyon);
                }
            }

            await _randevuRepository.SaveChangesAsync();

            // --- 3. FATURA ÜRETİMİ VE MAİL SÜREÇLERİ ---
            var fatura = new Fatura
            {
                RandevuId = randevu.Id,
                IscilikUcreti = iscilikUcreti,
                ToplamParcaMaliyeti = toplamParcaMaliyeti,
                KesimTarihi = DateTime.Now
            };

            await _faturaRepository.AddAsync(fatura);
            await _faturaRepository.SaveChangesAsync();

            randevu.Kullanici = await _kullaniciRepository.GetAsync(m => m.Id == randevu.KullaniciId);
            randevu.Arac = await _aracRepository.GetAsync(a => a.Id == randevu.AracId);
            randevu.HizmetTipi = hizmet;
            randevu.RandevuParcalari = (await _randevuParcaRepository.GetAllAsync(rp => rp.RandevuId == randevu.Id)).ToList();

            string pdfYolu = await _pdfFaturaService.FaturaUretVeKaydetAsync(fatura, randevu, faturayaGidecekParcalar);

            fatura.PdfDosyaYolu = pdfYolu;
            await _faturaRepository.UpdateAsync(fatura);
            await _faturaRepository.SaveChangesAsync();

            await _publishEndpoint.Publish(new FaturaKesildiEvent
            {
                FaturaId = fatura.Id,
                RandevuId = randevu.Id,
                MusteriEmail = randevu.Kullanici?.Email ?? string.Empty,
                MusteriAdSoyad = randevu.Kullanici?.AdSoyad ?? "Sayın Müşterimiz",
                AracPlaka = randevu.Arac?.Plaka ?? string.Empty,
                ToplamTutar = fatura.GenelToplam,
                KesimTarihi = fatura.KesimTarihi,
                IscilikUcreti = fatura.IscilikUcreti,
                Parca = fatura.ToplamParcaMaliyeti,
                PdfDosyaYolu = pdfYolu
            });

            return fatura;
        }

        public async Task<IEnumerable<Randevu>> GetUstaninGunlukRandevulariAsync(int kullaniciId)
        {
            var ustalar = await _ustaRepository.GetAllAsync();
            var usta = ustalar.FirstOrDefault(u => u.KullaniciId == kullaniciId);

            if (usta == null)
            {
                return Enumerable.Empty<Randevu>();
            }

            // Artık bütün işi Repository'deki özel metoda devrettik!
            // Türkiye saatine göre 'Bugün' neyse onu gönderiyoruz.
            var ustaRandevulari = await _randevuRepository.UstaninGunlukRandevulariniGetirAsync(usta.Id, DateTime.Now);

            return ustaRandevulari;
        }

        public async Task<IEnumerable<Randevu>> GetGunlukTumRandevularAsync()
        {
            var tumRandevular = await _randevuRepository.TumRandevulariIliskileriyleGetirAsync();

            // Sadece BUGÜNE ait TÜM randevuları saatine göre sıralayarak getir
            return tumRandevular
             .Where(r => r.Tarih.Date == DateTime.Today.Date)
             .OrderBy(r => r.Tarih)
             .ToList();
        }


    }
    }


