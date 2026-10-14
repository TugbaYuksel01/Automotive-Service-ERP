using Microsoft.EntityFrameworkCore;
using OtoServis.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Text.Json;

namespace OtoServis.Data
{

    public class ServisDbContext : DbContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public ServisDbContext(DbContextOptions<ServisDbContext> options, IHttpContextAccessor httpContextAccessor = null) : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }


        public DbSet<Usta> Ustalar { get; set; }
        public DbSet<HizmetTipi> HizmetTipleri { get; set; }
        public DbSet<Istasyon> Istasyonlar { get; set; }
        public DbSet<Randevu> Randevular { get; set; }

        public DbSet<Arac> Araclar { get; set; }
        public DbSet<YedekParca> YedekParcalar { get; set; }
        public DbSet<RandevuParca> RandevuParcalari { get; set; }
        public DbSet<Fatura> Faturalar { get; set; }

        public DbSet<Kullanici> Kullanicilar { get; set; }

        public DbSet<SistemLog> SistemLoglari { get; set; }


        // Bu kod sayesinde SQL Server'a "Bir veriyi sildiğimde zincirleme katliam yapma, sadece o veriyi sil (veya izin verme)" demiş olduk.
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Eğer Identity veya başka eklentiler varsa bu satır şarttır

            // İlk Admin'i sisteme gömüyoruz
            modelBuilder.Entity<Kullanici>().HasData(new Kullanici  // İlk adminnin dbye yansıması için Migragation yapmalıyız 
            {
                Id = 1,
                AdSoyad = "Sistem Yöneticisi",
                Email = "admin@otoservis.com",
                Rol = "SuperAdmin",
                // Gerçek projede "Sifre123!" değerinin BCrypt/SHA256 ile hashlenmiş halini buraya koymalısın
                SifreHash = "$2a$11$B599TyWE.UfpdESVM/cvROCpi071XP8hyn5ARCEdsZmAz3GSL4bJy"
            });

            // Aynı telefon numarasıyla ikinci bir müşteri eklenmesini yasaklar
            modelBuilder.Entity<Kullanici>().HasIndex(m => m.Telefon).IsUnique();

            // Aynı plakayla ikinci bir araç eklenmesini yasaklar
            modelBuilder.Entity<Arac>().HasIndex(a => a.Plaka).IsUnique();
            // 1. Müşteri ve Araç Korumaları
            modelBuilder.Entity<Kullanici>().HasIndex(m => m.Telefon).IsUnique();
            modelBuilder.Entity<Arac>().HasIndex(a => a.Plaka).IsUnique();

            // 2. Sabit Veri (Master Data) Korumaları
            modelBuilder.Entity<HizmetTipi>().HasIndex(h => h.Ad).IsUnique();
            modelBuilder.Entity<Istasyon>().HasIndex(i => i.Ad).IsUnique();
            modelBuilder.Entity<YedekParca>().HasIndex(y => y.ParcaAdi).IsUnique();

            // 3. Fatura - Randevu Bire-Bir İlişkisi (Aynı randevuya çift fatura kesilemez)
            modelBuilder.Entity<Fatura>()
                .HasIndex(f => f.RandevuId)
                .IsUnique();



            // SQL'in bahsettiği UyumluHizmetTipiId döngüsünü kırıyoruz (Silmeyi kısıtla)
            modelBuilder.Entity<YedekParca>()
                .HasOne(y => y.UyumluHizmetTipi)
                .WithMany(h => h.UyumluParcalar)
                .HasForeignKey(y => y.UyumluHizmetTipiId)
                .OnDelete(DeleteBehavior.Restrict);

            // İkinci bir patlama riskine karşı Randevu-Parça ilişkisini de kısıtlıyoruz
            modelBuilder.Entity<RandevuParca>()
                .HasOne(rp => rp.YedekParca)
                .WithMany(y => y.RandevuParcalari)
                .HasForeignKey(rp => rp.YedekParcaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RandevuParca>()
                .HasOne(rp => rp.Randevu)
                .WithMany(r => r.RandevuParcalari)
                .HasForeignKey(rp => rp.RandevuId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        //DBYE YAPILAN ERİŞİMLERİ LOGLAMA 
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Veritabanına gitmek üzere olan değişiklikleri (Ekle/Güncelle/Sil) ChangeTracker ile havada yakala
            // Sonsuz döngü olmasın diye "SistemLog" tablosuna yapılan eklemeleri takip etmiyoruz.
            var degisiklikler = ChangeTracker.Entries()
                .Where(e => e.Entity.GetType().Name != nameof(SistemLog) &&
                           (e.State == EntityState.Added ||
                            e.State == EntityState.Modified ||
                            e.State == EntityState.Deleted))
                .ToList();

            var eklenecekLoglar = new List<SistemLog>();

            // Varsayılan değer (Worker vb. arka plan servisleri için)
            string aktifKullaniciId = "Sistem";

            // Ortada bir HTTP isteği (API çağrısı) varsa:
            if (_httpContextAccessor?.HttpContext != null)
            {
                // 1. Durum: Kullanıcı giriş yapmış (Token var)
                if (_httpContextAccessor.HttpContext.User?.Identity?.IsAuthenticated == true)
                {
                    aktifKullaniciId = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                    ?? _httpContextAccessor.HttpContext.User.FindFirst("id")?.Value
                                    ?? "Bilinmeyen Kullanici";
                }
                // 2. Durum: Kullanıcı giriş YAPMAMIŞ ama API'ye istek atmış (Anonim/Yetkisiz İstek)
                else
                {
                    // İsteği atan kişinin IP adresini yakala
                    var ipAdresi = _httpContextAccessor.HttpContext.Connection?.RemoteIpAddress?.ToString();
                    aktifKullaniciId = string.IsNullOrEmpty(ipAdresi) ? "Bilinmeyen IP" : $"Ziyaretci IP: {ipAdresi}";
                }
            }

            // Yakalanan her bir değişiklik için otomatik log satırı oluştur
            foreach (var kayit in degisiklikler)
            {
                var islemTipi = kayit.State switch
                {
                    EntityState.Added => "EKLEME",
                    EntityState.Modified => "GUNCELLEME",
                    EntityState.Deleted => "SILME",
                    _ => "BILINMEYEN"
                };

                var tabloAdi = kayit.Entity.GetType().Name;

                // 1. O anki kaydın verilerini al (Silinmişse eskileri, değilse yenileri al)
                var degerler = kayit.State == EntityState.Deleted ? kayit.OriginalValues : kayit.CurrentValues;

                // 2. Logda asla görünmemesi gereken hassas kolonları belirle
                var gizlenecekKolonlar = new[] { "Sifre", "SifreHash", "Password", "Token" };

                // 3. Okunabilir bir "Kayıt Tanımı" oluştur (Örn: "Ali Yılmaz (ali@mail.com)" veya "Id: 15")
                string kayitTanimi = "";
                var ad = degerler.Properties.Any(p => p.Name == "Ad") ? degerler["Ad"]?.ToString() : "";
                var soyad = degerler.Properties.Any(p => p.Name == "Soyad") ? degerler["Soyad"]?.ToString() : "";
                var email = degerler.Properties.Any(p => p.Name == "Email") ? degerler["Email"]?.ToString() : "";
                var id = degerler.Properties.Any(p => p.Name == "Id") ? degerler["Id"]?.ToString() : "";

                if (!string.IsNullOrEmpty(ad) || !string.IsNullOrEmpty(email))
                {
                    kayitTanimi = $"{ad} {soyad} {(!string.IsNullOrEmpty(email) ? $"({email})" : "")}".Trim();
                }
                else
                {
                    kayitTanimi = $"Id: {id}"; // Ad/Email yoksa sadece Id'sini yaz (Örn: Randevu tablosu)
                }

                // 4. İnsancıl Log Cümlesini İnşa Et
                string detayCumlesi = "";

                if (kayit.State == EntityState.Added)
                {
                    detayCumlesi = $"{tabloAdi} tablosuna yeni kayıt eklendi: {kayitTanimi}.";
                }
                else if (kayit.State == EntityState.Deleted)
                {
                    detayCumlesi = $"{tabloAdi} tablosundan bir kayıt silindi: {kayitTanimi}.";
                }
                else if (kayit.State == EntityState.Modified)
                {
                    var degisimler = new List<string>();
                    foreach (var prop in kayit.OriginalValues.Properties)
                    {
                        // Şifre vb. alanları loga yazma, atla!
                        if (gizlenecekKolonlar.Contains(prop.Name)) continue;

                        var eski = kayit.OriginalValues[prop]?.ToString();
                        var yeni = kayit.CurrentValues[prop]?.ToString();

                        if (eski != yeni)
                        {
                            degisimler.Add($"{prop.Name} ('{eski}' -> '{yeni}')");
                        }
                    }

                    // Eğer şifre hariç gerçekten değişen bir şey varsa yaz
                    if (degisimler.Any())
                    {
                        detayCumlesi = $"{tabloAdi} tablosunda {kayitTanimi} güncellendi. Değişenler: {string.Join(", ", degisimler)}.";
                    }
                    else
                    {
                        detayCumlesi = $"{tabloAdi} tablosunda {kayitTanimi} güncellendi. (Sadece hassas/gizli veriler değişti).";
                    }
                }

                eklenecekLoglar.Add(new SistemLog
                {
                    IslemTipi = islemTipi,
                    TabloAdi = tabloAdi,
                    Detay = detayCumlesi, // Tam istediğin gibi kısa, temiz ve okunaklı bir cümle!
                    Tarih = DateTime.UtcNow,
                    KullaniciId = aktifKullaniciId
                });
                               
            }

            // Eğer eklenecek log varsa onları da sıraya al
            if (eklenecekLoglar.Any())
            {
                await SistemLoglari.AddRangeAsync(eklenecekLoglar, cancellationToken);
            }

            // Asıl işlemi ve logları veritabanına kaydet, ardından sonucu döndür!
            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
