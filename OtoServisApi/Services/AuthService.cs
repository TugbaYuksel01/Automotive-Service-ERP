using BCrypt.Net; // Şifre hashleme için popüler bir kütüphane (NuGet: BCrypt.Net-Next)
using MassTransit;
using Microsoft.IdentityModel.Tokens;
using OtoServis.Data.Contracts;
using OtoServis.Data.Models;
using OtoServis.Data.Repositories;
using OtoServisApi.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace OtoServisApi.Services
{
    public class AuthService : IAuthService
    {
        private readonly IRepository<Kullanici> _kullaniciRepository;
        private readonly IRepository<Usta> _ustaRepository;
        private readonly IConfiguration _configuration;
        
        private readonly IPublishEndpoint _publishEndpoint; // Mail servisi yerine RabbitMQ geldi
        public AuthService(
            IRepository<Kullanici> kullaniciRepository,
            IConfiguration configuration,
           IRepository<Usta> ustaRepository,
            IPublishEndpoint publishEndpoint
            )
        {
            _kullaniciRepository = kullaniciRepository;
            _configuration = configuration;
            _ustaRepository = ustaRepository;
            _publishEndpoint = publishEndpoint;
        }

        public async Task<string> RegisterAsync(RegisterDto dto)
        {
            // 1. Bu e-postayla daha önce bir kayıt var mı? (Misafir de olabilir, müşteri de)
            var mevcutKullanici = await _kullaniciRepository.GetAsync(k => k.Email == dto.Email);

            if (mevcutKullanici != null)
            {
                // EĞER DAHA ÖNCE MİSAFİR OLARAK GELMİŞSE: Yeni satır açma, üstüne yaz!
                if (mevcutKullanici.Rol == "Misafir" || mevcutKullanici.Rol == "misafir" || string.IsNullOrEmpty(mevcutKullanici.SifreHash))
                {
                    mevcutKullanici.AdSoyad = dto.AdSoyad; 
                    mevcutKullanici.SifreHash = BCrypt.Net.BCrypt.HashPassword(dto.Sifre);
                    mevcutKullanici.Rol = "Musteri";

                    await _kullaniciRepository.UpdateAsync(mevcutKullanici);
                    await _kullaniciRepository.SaveChangesAsync();
                    return "Misafir kaydınız kalıcı müşteri hesabına dönüştürüldü.";
                }

                throw new Exception("Bu e-posta adresi ile zaten kayıtlı bir hesap bulunuyor.");
            }

            // 2. Hiç yoksa normal yeni kayıt aç
            var sifreHash = BCrypt.Net.BCrypt.HashPassword(dto.Sifre);
            var yeniKullanici = new Kullanici
            {
                AdSoyad = dto.AdSoyad,
                Email = dto.Email,
                SifreHash = sifreHash,
                Rol = dto.Rol
            };

            await _kullaniciRepository.AddAsync(yeniKullanici);
            await _kullaniciRepository.SaveChangesAsync();

            return "Kayıt işlemi başarıyla tamamlandı.";
        }

        public async Task<string> LoginAsync(LoginDto dto)
        {
            // KESİN ÇÖZÜM: E-postası eşleşen VEEEE Şifresi boş OLMAYAN (Gerçek) hesabı getir!
            var kullanici = await _kullaniciRepository.GetAsync(k => k.Email == dto.Email && k.SifreHash != null);

            if (kullanici == null || !BCrypt.Net.BCrypt.Verify(dto.Sifre, kullanici.SifreHash))
                throw new Exception("E-posta adresi veya şifre hatalı.");

            return TokenUret(kullanici);
        }

        public async Task<int> MisafirKaydetVeyaBulAsync(string adSoyad, string email, string telefon)
        {
            // 1. SORGULAMA: Veritabanında bu telefon numarasına VEYA e-postaya sahip biri var mı?
            var mevcutKullanicilar = await _kullaniciRepository.GetAllAsync(
                k => k.Telefon == telefon || k.Email == email);

            var musteri = mevcutKullanicilar.FirstOrDefault();

            // 2. BULUNDU DURUMU (Eski misafir veya zaten kayıtlı bir müşteri)
            if (musteri != null)
            {
                // GÜVENLİK KALKANI: Bulunan kişi Misafir DEĞİLSE (Müşteri, Usta, Admin vs. ise) hata fırlat
                if (musteri.Rol != "Misafir" && musteri.Rol != "misafir")
                {
                    throw new Exception("Bu e-posta adresiyle kayıtlı bir hesabınız bulunuyor. Lütfen önce giriş yapınız.");
                }

                // Eğer rolü cidden misafirse, eski ID'sini ver ki yeni randevu da onun üstüne yazılsın
                return musteri.Id;
            }

            // 3. BULUNAMADI DURUMU (Sisteme tamamen yabancı, ilk defa gelen kişi)
            var yeniMisafir = new Kullanici
            {
                AdSoyad = adSoyad,
                Telefon = telefon,
                Email = email,
                Rol = "Misafir", // Normal müşteri hesabından ayırmak için rolü Misafir yapıyoruz
                SifreHash = null // Şifre null kalıyor çünkü bu kişi sisteme bilinçli bir "Kayıt Ol" işlemi yapmadı
            };

            // 4. KAYIT İŞLEMİ: Yeni misafiri veritabanına ekle
            await _kullaniciRepository.AddAsync(yeniMisafir);
            _kullaniciRepository.SaveChangesAsync();

            // 5. YENİ ID'Yİ DÖNDÜR: Veritabanı tarafından atanan taze ID'yi Controller'a yolluyoruz.
            return yeniMisafir.Id;
        }





        public async Task<(bool BasariliMi, string Mesaj)> PersonelEkleAsync(PersonelEkleDto dto, string atanacakRol)
        {
            // 1. E-Posta Tekrarı Kontrolü
            var mevcutKullanici = await _kullaniciRepository.GetAsync(k => k.Email == dto.Email);
            if (mevcutKullanici != null)
            {
                return (false, "Bu e-posta adresiyle kayıtlı bir kullanıcı zaten var.");
            }

            // 2. Şifreyi Hashleme
            string hashliSifre = BCrypt.Net.BCrypt.HashPassword(dto.Sifre);

            // 3. Yeni Entity'yi Oluşturma
            var yeniPersonel = new Kullanici
            {
                AdSoyad = dto.AdSoyad,
                Email = dto.Email,
                Telefon = dto.Telefon,
                SifreHash = hashliSifre,
                Rol = atanacakRol
            };

            // 4. Veritabanına Kaydetme
            await _kullaniciRepository.AddAsync(yeniPersonel);
            await _kullaniciRepository.SaveChangesAsync(); // DİKKAT: Bu satır çalıştığı an yeniPersonel.Id veritabanı tarafından doldurulur!

            // 5. YENİ OTOMASYON: EĞER EKLENEN KİŞİ USTA İSE, VİTRİNE (USTALAR TABLOSUNA) DE EKLE
            if (atanacakRol.Equals("Usta", StringComparison.OrdinalIgnoreCase))
            {
                var yeniUsta = new Usta
                {
                    AdSoyad = yeniPersonel.AdSoyad,
                    KullaniciId = yeniPersonel.Id,

                    // YENİ DOKUNUŞ: Frontend'den gelen uzmanlık alanını kaydediyoruz.
                    // Eğer Frontend boş gönderdiyse "Genel Usta" varsayılanını atıyoruz.
                    UzmanlikAlani = !string.IsNullOrWhiteSpace(dto.UzmanlikAlani) ? dto.UzmanlikAlani : "Genel Usta"
                };

                await _ustaRepository.AddAsync(yeniUsta);
                await _ustaRepository.SaveChangesAsync();
            }

            return (true, $"{atanacakRol} rolündeki personel başarıyla oluşturuldu.");
        }



        public async Task<(bool BasariliMi, string Mesaj)> SifreSifirlamaKoduGonderAsync(SifremiUnuttumDto dto)
        {
            // KESİN ÇÖZÜM: Şifresi olan gerçek hesabı bul
            var kullanici = await _kullaniciRepository.GetAsync(k => k.Email == dto.Email && k.SifreHash != null );

            if (kullanici == null)
            {
                // Eğer o maille sadece Misafir varsa veya hiç yoksa
                return (false, "Bu e-posta adresiyle kayıtlı şifreli bir hesabınız bulunmuyor. Lütfen önce 'Kayıt Ol' sayfasından hesabınızı oluşturun.");
            }

            // Kırılmaz bir GUID (Token) üretiyoruz
            string resetToken = Guid.NewGuid().ToString();

            kullanici.SifreSifirlamaKodu = resetToken;
            kullanici.SifreSifirlamaKoduBitis = DateTime.Now.AddMinutes(15);

            await _kullaniciRepository.UpdateAsync(kullanici);
            await _kullaniciRepository.SaveChangesAsync();

            string frontendSifirlamaLinki = $"http://localhost:5173/sifre-sifirla?token={Uri.EscapeDataString(resetToken)}&email={Uri.EscapeDataString(kullanici.Email)}";

            string mailIcerik = $"<h2>Şifre Sıfırlama Talebi</h2>" +
                                $"<p>Yeni şifrenizi belirlemek için aşağıdaki bağlantıya tıklayınız:</p>" +
                                $"<a href='{frontendSifirlamaLinki}' style='padding: 10px 20px; background-color: #007bff; color: white; text-decoration: none; border-radius: 5px; display: inline-block;'>Şifremi Sıfırla</a>" +
                                $"<br><br><p>Bu bağlantı 15 dakika sonra geçerliliğini yitirecektir.</p>";

            var mailEvent = new SifreSifirlamaIstendiEvent
            {
                AliciEmail = kullanici.Email,
                Konu = "Oto Servis - Şifre Sıfırlama Bağlantısı",
                HtmlIcerik = mailIcerik
            };

            await _publishEndpoint.Publish(mailEvent);

            return (true, "Şifre sıfırlama bağlantısı e-posta adresinize gönderildi.");
        }




        public async Task<(bool BasariliMi, string Mesaj)> YeniSifreyiKaydetAsync(SifreSifirlaDto dto)
        {
            var kullanici = await _kullaniciRepository.GetAsync(k => k.Email == dto.Email);

            if (kullanici == null || kullanici.SifreSifirlamaKodu != dto.Token)
            {
                return (false, "Geçersiz e-posta adresi veya sıfırlama kodu.");
            }

            if (kullanici.SifreSifirlamaKoduBitis < DateTime.Now)
            {
                return (false, "Sıfırlama kodunun süresi dolmuş. Lütfen tekrar talep ediniz.");
            }

            // Kod doğru ve süresi geçmemiş! Şifreyi BCrypt ile eziyoruz
            kullanici.SifreHash = BCrypt.Net.BCrypt.HashPassword(dto.YeniSifre);

            // Güvenlik için kullanılmış kodu temizliyoruz
            kullanici.SifreSifirlamaKodu = null;
            kullanici.SifreSifirlamaKoduBitis = null;

            await _kullaniciRepository.UpdateAsync(kullanici);
            await _kullaniciRepository.SaveChangesAsync();

            return (true, "Şifreniz başarıyla güncellendi. Yeni şifrenizle giriş yapabilirsiniz.");
        }



        private string TokenUret(Kullanici kullanici)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var gizliAnahtar = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));
            var imzaKredisi = new SigningCredentials(gizliAnahtar, SecurityAlgorithms.HmacSha256);

            // Token'ın içine gömülecek kimlik bilgileri (Yaka Kartı detayları)
            // AuthService.cs içindeki TokenUret metodu
            var claims = new[]
            {
                // Sadece değişmeyen statik kimlik ve yetki bilgileri
                new Claim(ClaimTypes.NameIdentifier, kullanici.Id.ToString()),
                new Claim(ClaimTypes.Role, kullanici.Rol)
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(3), // Token 3 saat geçerli olsun
                signingCredentials: imzaKredisi
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        public async Task<Kullanici> GetProfilBilgileriAsync(int kullaniciId)
        {
            return await _kullaniciRepository.GetAsync(k => k.Id == kullaniciId);
        }

        public async Task<(bool BasariliMi, string Mesaj)> ProfilGuncelleAsync(int kullaniciId, ProfilGuncelleDto dto)
        {
            var kullanici = await _kullaniciRepository.GetAsync(k => k.Id == kullaniciId);
            if (kullanici == null)
                return (false, "Kullanıcı bulunamadı.");

            // E-posta başkası tarafından kullanılıyor mu kontrolü
            var emailKontrol = await _kullaniciRepository.GetAsync(k => k.Email == dto.Email && k.Id != kullaniciId);
            if (emailKontrol != null)
                return (false, "Bu e-posta adresi başka bir kullanıcı tarafından kullanılmaktadır.");

            // Telefon başkası tarafından kullanılıyor mu kontrolü (Eğer telefon girildiyse)
            if (!string.IsNullOrEmpty(dto.Telefon))
            {
                var telKontrol = await _kullaniciRepository.GetAsync(k => k.Telefon == dto.Telefon && k.Id != kullaniciId);
                if (telKontrol != null)
                    return (false, "Bu telefon numarası başka bir kullanıcı tarafından kullanılmaktadır.");
            }

            // Bilgileri güncelle
            kullanici.AdSoyad = dto.AdSoyad;
            kullanici.Email = dto.Email;
            kullanici.Telefon = dto.Telefon;

            await _kullaniciRepository.UpdateAsync(kullanici);
            await _kullaniciRepository.SaveChangesAsync();

            return (true, "Profil bilgileriniz başarıyla güncellendi.");
        }

        public async Task<(bool BasariliMi, string Mesaj)> SifreGuncelleAsync(int kullaniciId, SifreGuncelleDto dto)
        {
            // 1. Kullanıcıyı bul
            var kullanici = await _kullaniciRepository.GetAsync(k => k.Id == kullaniciId);
            if (kullanici == null) return (false, "Kullanıcı bulunamadı.");

            // 2. Mevcut şifreyi doğrula
            if (!BCrypt.Net.BCrypt.Verify(dto.MevcutSifre, kullanici.SifreHash))
            {
                return (false, "Mevcut şifrenizi yanlış girdiniz.");
            }

            // 3. Yeni şifreyi hashle ve kaydet
            kullanici.SifreHash = BCrypt.Net.BCrypt.HashPassword(dto.YeniSifre);
            await _kullaniciRepository.UpdateAsync(kullanici);
            await _kullaniciRepository.SaveChangesAsync();

            return (true, "Şifreniz başarıyla güncellendi.");
        }
    }
}