using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using OtoServis.Data.Models;
using OtoServis.Data.Repositories;
using OtoServis.Contracts.Events;

namespace OtoServisApi.Services
{
    public class KaynakPlanlamaMotoru : BackgroundService
    {
        // Arka plan servisleri sürekli çalıştığı (Singleton) için, 
        // veritabanı gibi anlık (Scoped) servislere ulaşmak adına bir fabrika kullanmalıyız.
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<KaynakPlanlamaMotoru> _logger;

        public KaynakPlanlamaMotoru(IServiceScopeFactory scopeFactory, ILogger<KaynakPlanlamaMotoru> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Kaynak Planlama Motoru çalışmaya başladı...");

            // API ayakta kaldığı sürece bu döngü çalışmaya devam eder
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PlanlamaYapAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Planlama motorunda bir hata oluştu.");
                }

                // Motor havuzu çok yormamak için her 1 dakikada (60.000 ms) bir çalışır. 
                // Test için bu süreyi 10000 (10 saniye) yapabilirsin.
                await Task.Delay(60000, stoppingToken);
            }
        }






        private async Task PlanlamaYapAsync()
        {
            // --- 0. HAZIRLIK VE VERİ TABANI BAĞLANTILARI ---
            // Singleton (Sürekli çalışan) bir servisin içinde, anlık veritabanı işlemi yapabilmek için geçici bir 'Scope' (alan) yaratıyoruz.
            using var veritabaniBaglantiAlani = _scopeFactory.CreateScope();
            var randevuTablosu = veritabaniBaglantiAlani.ServiceProvider.GetRequiredService<IRepository<Randevu>>();
            var ustaTablosu = veritabaniBaglantiAlani.ServiceProvider.GetRequiredService<IRepository<Usta>>();
            var istasyonTablosu = veritabaniBaglantiAlani.ServiceProvider.GetRequiredService<IRepository<Istasyon>>();
            var hizmetTablosu = veritabaniBaglantiAlani.ServiceProvider.GetRequiredService<IRepository<HizmetTipi>>();

            // Mail, SMS veya SignalR bildirimleri gönderebilmek için mesaj kuyruğu (RabbitMQ) tetikleyicisini alıyoruz.
            var olayFirlatici = veritabaniBaglantiAlani.ServiceProvider.GetRequiredService<IPublishEndpoint>();

            // EF Core Tracking (Takip Sistemi) devrede! Çekilen tüm listeler RAM'de takip edilir.
            // Bu nesnelerde yaptığımız her "=" (atama) işlemi RAM'e yazılır, Update metoduna gerek kalmaz.
            var sistemdekiTumRandevular = (await randevuTablosu.GetAllAsync(null, r => r.HizmetTipi, r => r.Kullanici, r => r.Arac)).ToList();
            var dukkanUstalar = (await ustaTablosu.GetAllAsync()).ToList();
            var dukkanIstasyonlari = (await istasyonTablosu.GetAllAsync()).ToList();

            // Sürekli veritabanına sorgu atmamak için, hizmetlerin "sürelerini" bir sözlük (Dictionary) olarak RAM'e alıyoruz.
            var hizmetlerinGerektirdigiUstaSureleri = (await hizmetTablosu.GetAllAsync())
                .ToDictionary(h => h.Id, h => h.UstaSureDakika > 0 ? h.UstaSureDakika : 30);

            var hizmetlerinGerektirdigiLiftSureleri = (await hizmetTablosu.GetAllAsync())
                .ToDictionary(h => h.Id, h => h.LiftSureDakika > 0 ? h.LiftSureDakika : 60);

            // KRONOLOJİK VE ÖNCELİKLİ SIRALAMA
            // Sistem önce saati gelmiş olanları, saati aynı olanlar arasında ise ACİL olanları en öne alır. Çift atamayı imkansız kılar.
            var sirasiGelenBekleyenRandevular = sistemdekiTumRandevular
                .Where(r => r.Durum == RandevuDurumu.Bekliyor && r.Tarih > DateTime.Now)
                .OrderBy(r => r.Tarih)
                .ThenByDescending(r => r.AcilMi ? 1 : 0)
                .ToList();

            // 💡 MATEMATİKSEL KESİN ÇAKIŞMA FORMÜLÜ (Yerel Fonksiyon)
            // İki farklı zaman aralığının (Örn: 16:30-17:00 ile 16:45-17:15) birbirini ezip ezmediğini hesaplar.
            bool IkiZamanAraligiKesisiyorMu(DateTime birinciBaslangic, int birinciSure, DateTime ikinciBaslangic, int ikinciSure)
            {
                var birinciBitis = birinciBaslangic.AddMinutes(birinciSure);
                var ikinciBitis = ikinciBaslangic.AddMinutes(ikinciSure);
                // Kural: Birincinin başlangıcı, ikincinin bitişinden önceyse VE ikincinin başlangıcı birincinin bitişinden önceyse çakışma vardır!
                return birinciBaslangic < ikinciBitis && ikinciBaslangic < birinciBitis;
            }

            // --- AŞAMA 1: STANDART PLANLAMA (KİMSENİN ZARAR GÖRMEYECEĞİ BOŞLUKLARI DOLDURMA) ---
            var henuzUstaVeyaLiftAtanmamisRandevular = sirasiGelenBekleyenRandevular.Where(r => r.UstaId == null || r.IstasyonId == null).ToList();

            foreach (var islenecekRandevu in henuzUstaVeyaLiftAtanmamisRandevular)
            {
                int gerekenUstaSuresi = hizmetlerinGerektirdigiUstaSureleri.ContainsKey(islenecekRandevu.HizmetTipiId) ? hizmetlerinGerektirdigiUstaSureleri[islenecekRandevu.HizmetTipiId] : 30;
                int gerekenLiftSuresi = hizmetlerinGerektirdigiLiftSureleri.ContainsKey(islenecekRandevu.HizmetTipiId) ? hizmetlerinGerektirdigiLiftSureleri[islenecekRandevu.HizmetTipiId] : 60;

                // O saat aralığında BAŞKA HİÇBİR İŞİ OLMAYAN ve UZMANLIĞI UYAN ilk ustayı buluruz.
                var oSaatteTamamenBosOlanUsta = dukkanUstalar.FirstOrDefault(ustaAdayi =>
                    ustaAdayi.UzmanlikAlani == islenecekRandevu.HizmetTipi?.GerekenUzmanlik &&
                    !sistemdekiTumRandevular.Any(digerRandevu =>
                        digerRandevu.Id != islenecekRandevu.Id &&
                        digerRandevu.UstaId == ustaAdayi.Id &&
                        (digerRandevu.Durum == RandevuDurumu.Bekliyor || digerRandevu.Durum == RandevuDurumu.Islemde) &&
                        IkiZamanAraligiKesisiyorMu(islenecekRandevu.Tarih, gerekenUstaSuresi, digerRandevu.GerceklesenBaslangic ?? digerRandevu.Tarih, hizmetlerinGerektirdigiUstaSureleri.ContainsKey(digerRandevu.HizmetTipiId) ? hizmetlerinGerektirdigiUstaSureleri[digerRandevu.HizmetTipiId] : 30)
                    ));

                // O saat aralığında ÜZERİNDE ARAÇ OLMAYAN ilk boş lifti buluruz.
                var oSaatteTamamenBosOlanIstasyon = dukkanIstasyonlari.FirstOrDefault(istasyonAdayi =>
                    istasyonAdayi.MusaitMi &&
                    !sistemdekiTumRandevular.Any(digerRandevu =>
                        digerRandevu.Id != islenecekRandevu.Id &&
                        digerRandevu.IstasyonId == istasyonAdayi.Id &&
                        (digerRandevu.Durum == RandevuDurumu.Bekliyor || digerRandevu.Durum == RandevuDurumu.Islemde) &&
                        IkiZamanAraligiKesisiyorMu(islenecekRandevu.Tarih, gerekenLiftSuresi, digerRandevu.GerceklesenBaslangic ?? digerRandevu.Tarih, hizmetlerinGerektirdigiLiftSureleri.ContainsKey(digerRandevu.HizmetTipiId) ? hizmetlerinGerektirdigiLiftSureleri[digerRandevu.HizmetTipiId] : 60)
                    ));

                // Hem boş usta hem boş lift bulunduysa atamayı yap
                if (oSaatteTamamenBosOlanUsta != null && oSaatteTamamenBosOlanIstasyon != null)
                {
                    islenecekRandevu.UstaId = oSaatteTamamenBosOlanUsta.Id;
                    islenecekRandevu.IstasyonId = oSaatteTamamenBosOlanIstasyon.Id;
                    _logger.LogInformation($"[STANDART ATAMA] #{islenecekRandevu.Id} | Tarih: {islenecekRandevu.Tarih:HH:mm} | Usta: {oSaatteTamamenBosOlanUsta.AdSoyad}");
                }
            }

            // --- AŞAMA 2: ACİL DURUM MÜDAHALE TİMİ (MİNİMUM HASAR & PLAN A/B/C YÖNETİMİ) ---
            var krizYaratanAcilRandevular = sirasiGelenBekleyenRandevular.Where(r => r.UstaId == null && r.AcilMi).ToList();

            TimeSpan dukkanAcilisSaati = new TimeSpan(9, 0, 0);
            TimeSpan dukkanKapanisSaati = new TimeSpan(18, 0, 0);
            TimeSpan ogleMolasiBaslangici = new TimeSpan(12, 0, 0);
            TimeSpan ogleMolasiBitisi = new TimeSpan(13, 0, 0);

            foreach (var acilVaka in krizYaratanAcilRandevular)
            {
                var buIseUygunUzmanUstalar = dukkanUstalar.Where(u => u.UzmanlikAlani == acilVaka.HizmetTipi?.GerekenUzmanlik).ToList();
                if (!buIseUygunUzmanUstalar.Any()) continue;

                int acilVakaninSureIhtiyaci = hizmetlerinGerektirdigiUstaSureleri.ContainsKey(acilVaka.HizmetTipiId) ? hizmetlerinGerektirdigiUstaSureleri[acilVaka.HizmetTipiId] : 30;

                (Usta SecilenUsta, Istasyon SecilenLift, List<Randevu> MagdurEdilecekMusteriler) enAzHasarliSenaryo = (null, null, null);
                int minimumMagdurMusteriSayisi = int.MaxValue;

                foreach (var ustaAdayi in buIseUygunUzmanUstalar)
                {
                    foreach (var istasyonAdayi in dukkanIstasyonlari.Where(i => i.MusaitMi))
                    {
                        var buSenaryodaEzilenMusteriler = sistemdekiTumRandevular.Where(digerRandevu =>
                            digerRandevu.Id != acilVaka.Id &&
                            !digerRandevu.AcilMi &&
                            digerRandevu.Durum == RandevuDurumu.Bekliyor &&
                            (
                              (digerRandevu.UstaId == ustaAdayi.Id && IkiZamanAraligiKesisiyorMu(acilVaka.Tarih, acilVakaninSureIhtiyaci, digerRandevu.Tarih, hizmetlerinGerektirdigiUstaSureleri.ContainsKey(digerRandevu.HizmetTipiId) ? hizmetlerinGerektirdigiUstaSureleri[digerRandevu.HizmetTipiId] : 30)) ||
                              (digerRandevu.IstasyonId == istasyonAdayi.Id && IkiZamanAraligiKesisiyorMu(acilVaka.Tarih, acilVakaninSureIhtiyaci, digerRandevu.Tarih, hizmetlerinGerektirdigiLiftSureleri.ContainsKey(digerRandevu.HizmetTipiId) ? hizmetlerinGerektirdigiLiftSureleri[digerRandevu.HizmetTipiId] : 60))
                            )
                        ).ToList();

                        if (buSenaryodaEzilenMusteriler.Count == 0)
                        {
                            enAzHasarliSenaryo = (ustaAdayi, istasyonAdayi, buSenaryodaEzilenMusteriler);
                            break;
                        }

                        if (buSenaryodaEzilenMusteriler.Count < minimumMagdurMusteriSayisi)
                        {
                            minimumMagdurMusteriSayisi = buSenaryodaEzilenMusteriler.Count;
                            enAzHasarliSenaryo = (ustaAdayi, istasyonAdayi, buSenaryodaEzilenMusteriler);
                        }
                    }
                    if (enAzHasarliSenaryo.MagdurEdilecekMusteriler != null && enAzHasarliSenaryo.MagdurEdilecekMusteriler.Count == 0) break;
                }

                if (enAzHasarliSenaryo.SecilenUsta == null) continue;

                acilVaka.UstaId = enAzHasarliSenaryo.SecilenUsta.Id;
                acilVaka.IstasyonId = enAzHasarliSenaryo.SecilenLift.Id;

                foreach (var ezilenNormalRandevu in enAzHasarliSenaryo.MagdurEdilecekMusteriler)
                {
                    int ezilenMusterininUstaSuresi = hizmetlerinGerektirdigiUstaSureleri.ContainsKey(ezilenNormalRandevu.HizmetTipiId) ? hizmetlerinGerektirdigiUstaSureleri[ezilenNormalRandevu.HizmetTipiId] : 30;
                    int ezilenMusterininLiftSuresi = hizmetlerinGerektirdigiLiftSureleri.ContainsKey(ezilenNormalRandevu.HizmetTipiId) ? hizmetlerinGerektirdigiLiftSureleri[ezilenNormalRandevu.HizmetTipiId] : 60;

                    var aPlaniIcinBosUsta = dukkanUstalar.FirstOrDefault(u =>
                        u.Id != enAzHasarliSenaryo.SecilenUsta.Id &&
                        u.UzmanlikAlani == ezilenNormalRandevu.HizmetTipi?.GerekenUzmanlik &&
                        !sistemdekiTumRandevular.Any(d => d.Id != ezilenNormalRandevu.Id && d.UstaId == u.Id && (d.Durum == RandevuDurumu.Bekliyor || d.Durum == RandevuDurumu.Islemde) &&
                        IkiZamanAraligiKesisiyorMu(ezilenNormalRandevu.Tarih, ezilenMusterininUstaSuresi, d.GerceklesenBaslangic ?? d.Tarih, hizmetlerinGerektirdigiUstaSureleri.ContainsKey(d.HizmetTipiId) ? hizmetlerinGerektirdigiUstaSureleri[d.HizmetTipiId] : 30))
                    );

                    var aPlaniIcinBosLift = dukkanIstasyonlari.FirstOrDefault(i =>
                        i.Id != enAzHasarliSenaryo.SecilenLift.Id && i.MusaitMi &&
                        !sistemdekiTumRandevular.Any(d => d.Id != ezilenNormalRandevu.Id && d.IstasyonId == i.Id && (d.Durum == RandevuDurumu.Bekliyor || d.Durum == RandevuDurumu.Islemde) &&
                        IkiZamanAraligiKesisiyorMu(ezilenNormalRandevu.Tarih, ezilenMusterininLiftSuresi, d.GerceklesenBaslangic ?? d.Tarih, hizmetlerinGerektirdigiLiftSureleri.ContainsKey(d.HizmetTipiId) ? hizmetlerinGerektirdigiLiftSureleri[d.HizmetTipiId] : 60))
                    );

                    if (aPlaniIcinBosUsta != null && aPlaniIcinBosLift != null)
                    {
                        ezilenNormalRandevu.UstaId = aPlaniIcinBosUsta.Id;
                        ezilenNormalRandevu.IstasyonId = aPlaniIcinBosLift.Id;
                        _logger.LogInformation($"[PLAN A - KUSURSUZ ÇÖZÜM] #{acilVaka.Id} acili sebebiyle #{ezilenNormalRandevu.Id} nolu iş saati saniye değişmeden {aPlaniIcinBosUsta.AdSoyad}'a devredildi.");
                    }
                    else
                    {
                        var orijinalSaat = ezilenNormalRandevu.Tarih; // Değişmeden önceki orijinal saat

                        ezilenNormalRandevu.Tarih = acilVaka.Tarih.AddMinutes(acilVakaninSureIhtiyaci);

                        if (ezilenNormalRandevu.Tarih.TimeOfDay >= ogleMolasiBaslangici && ezilenNormalRandevu.Tarih.TimeOfDay < ogleMolasiBitisi)
                        {
                            ezilenNormalRandevu.Tarih = ezilenNormalRandevu.Tarih.Date.Add(ogleMolasiBitisi);
                        }

                        if (ezilenNormalRandevu.Tarih.TimeOfDay >= dukkanKapanisSaati)
                        {
                            var ertesiSabah = ezilenNormalRandevu.Tarih.Date.AddDays(1).Add(dukkanAcilisSaati);
                            if (ertesiSabah.DayOfWeek == DayOfWeek.Sunday) ertesiSabah = ertesiSabah.AddDays(1);

                            ezilenNormalRandevu.Tarih = ertesiSabah;
                            _logger.LogWarning($"[PLAN C - VIP ERTELEME] Mesai aşıldı! #{ezilenNormalRandevu.Id} nolu iş ertesi gün sabah {ertesiSabah:dd.MM.yyyy HH:mm} saatine VIP statüsünde taşındı.");
                            await olayFirlatici.Publish(new RandevuKaydirildiEvent
                            {
                                RandevuId = ezilenNormalRandevu.Id,
                                MusteriEmail = ezilenNormalRandevu.Kullanici?.Email ?? "musteri@ornek.com",
                                MusteriAdSoyad = ezilenNormalRandevu.Kullanici?.AdSoyad ?? "Sayın Müşterimiz",
                                AracPlaka = ezilenNormalRandevu.Arac?.Plaka ?? string.Empty,
                                EskiTarih = orijinalSaat,
                                YeniTarih = ertesiSabah,
                                Tip = KaydirmaTipi.VipErtesiGun,
                                Aciklama = "İşlem atölye mesaimizi aştığı için randevunuz ertesi gün sabah 'VIP İlk Araç' statüsüne taşınmıştır."
                            });
                        }
                        else
                        {
                            _logger.LogWarning($"[PLAN B - ERTELEME] #{ezilenNormalRandevu.Id} nolu iş {ezilenNormalRandevu.Tarih:HH:mm} saatine ötelendi.");
                            await olayFirlatici.Publish(new RandevuKaydirildiEvent
                            {
                                RandevuId = ezilenNormalRandevu.Id,
                                MusteriEmail = ezilenNormalRandevu.Kullanici?.Email ?? "musteri@ornek.com",
                                MusteriAdSoyad = ezilenNormalRandevu.Kullanici?.AdSoyad ?? "Sayın Müşterimiz",
                                AracPlaka = ezilenNormalRandevu.Arac?.Plaka ?? string.Empty,
                                EskiTarih = orijinalSaat,
                                YeniTarih = ezilenNormalRandevu.Tarih,
                                Tip = KaydirmaTipi.KrizErtelemesi,
                                Aciklama = "Tüm birimlerimizdeki acil yoğunluk sebebiyle randevu saatiniz zorunlu olarak kaydırılmıştır."
                            });

                        }
                    }
                }
            }

            // --- AŞAMA 3: TRAFİK POLİSİ (ANLIK YÜK DENGELEME) ---
            var saatiGeldigiHaldeBaslayamayanRandevular = sistemdekiTumRandevular
                .Where(r => r.UstaId != null && r.Durum == RandevuDurumu.Bekliyor && r.Tarih <= DateTime.Now.AddMinutes(5))
                .ToList();

            var suAnAktifAracTamirEdenUstaIdleri = sistemdekiTumRandevular
                .Where(r => r.Durum == RandevuDurumu.Islemde && r.UstaId != null)
                .Select(r => r.UstaId.Value)
                .Distinct()
                .ToList();

            foreach (var gecikenIs in saatiGeldigiHaldeBaslayamayanRandevular)
            {
                if (suAnAktifAracTamirEdenUstaIdleri.Contains(gecikenIs.UstaId.Value))
                {
                    int gecikenIsinSuresi = hizmetlerinGerektirdigiUstaSureleri.ContainsKey(gecikenIs.HizmetTipiId) ? hizmetlerinGerektirdigiUstaSureleri[gecikenIs.HizmetTipiId] : 30;

                    var kurtariciBosUsta = dukkanUstalar.FirstOrDefault(u =>
                        !suAnAktifAracTamirEdenUstaIdleri.Contains(u.Id) &&
                        u.UzmanlikAlani == gecikenIs.HizmetTipi?.GerekenUzmanlik &&
                        !sistemdekiTumRandevular.Any(d => d.Id != gecikenIs.Id && d.UstaId == u.Id && d.Durum == RandevuDurumu.Bekliyor &&
                        IkiZamanAraligiKesisiyorMu(gecikenIs.Tarih, gecikenIsinSuresi, d.Tarih, hizmetlerinGerektirdigiUstaSureleri.ContainsKey(d.HizmetTipiId) ? hizmetlerinGerektirdigiUstaSureleri[d.HizmetTipiId] : 30))
                    );

                    if (kurtariciBosUsta != null)
                    {
                        var eskiUstaninAdi = dukkanUstalar.First(u => u.Id == gecikenIs.UstaId).AdSoyad;
                        gecikenIs.UstaId = kurtariciBosUsta.Id;
                        suAnAktifAracTamirEdenUstaIdleri.Add(kurtariciBosUsta.Id);
                        _logger.LogWarning($"[TRAFİK POLİSİ] {eskiUstaninAdi} adlı usta meşgul! #{gecikenIs.Id} nolu araç anında {kurtariciBosUsta.AdSoyad} adlı ustaya devredildi.");
                    }
                }
            }

            await randevuTablosu.SaveChangesAsync();
        }
    }
}
    

