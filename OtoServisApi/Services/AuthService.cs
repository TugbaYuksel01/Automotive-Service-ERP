using BCrypt.Net; 
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
        private readonly IPublishEndpoint _publishEndpoint;

        public AuthService(
            IRepository<Kullanici> kullaniciRepository,
            IConfiguration configuration,
            IRepository<Usta> ustaRepository,
            IPublishEndpoint publishEndpoint)
        {
            _kullaniciRepository = kullaniciRepository;
            _configuration = configuration;
            _ustaRepository = ustaRepository;
            _publishEndpoint = publishEndpoint;
        }

        public async Task<string> RegisterAsync(RegisterDto dto)
        {
            var mevcutKullanici = await _kullaniciRepository.GetAsync(k => k.Email == dto.Email);

            if (mevcutKullanici != null)
            {
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
            var kullanici = await _kullaniciRepository.GetAsync(k => k.Email == dto.Email && k.SifreHash != null);

            if (kullanici == null || !BCrypt.Net.BCrypt.Verify(dto.Sifre, kullanici.SifreHash))
                throw new Exception("E-posta adresi veya şifre hatalı.");

            return TokenUret(kullanici);
        }

        public async Task<int> MisafirKaydetVeyaBulAsync(string adSoyad, string email, string telefon)
        {
            var mevcutKullanicilar = await _kullaniciRepository.GetAllAsync(
                k => k.Telefon == telefon || k.Email == email);

            var musteri = mevcutKullanicilar.FirstOrDefault();

            if (musteri != null)
            {
                if (musteri.Rol != "Misafir" && musteri.Rol != "misafir")
                {
                    throw new Exception("Bu e-posta adresiyle kayıtlı bir hesabınız bulunuyor. Lütfen önce giriş yapınız.");
                }
                return musteri.Id;
            }

            var yeniMisafir = new Kullanici
            {
                AdSoyad = adSoyad,
                Telefon = telefon,
                Email = email,
                Rol = "Misafir",
                SifreHash = null 
            };

            await _kullaniciRepository.AddAsync(yeniMisafir);
            await _kullaniciRepository.SaveChangesAsync();

            return yeniMisafir.Id;
        }

        public async Task<(bool BasariliMi, string Mesaj)> PersonelEkleAsync(PersonelEkleDto dto, string atanacakRol)
        {
            var mevcutKullanici = await _kullaniciRepository.GetAsync(k => k.Email == dto.Email);
            if (mevcutKullanici != null)
            {
                return (false, "Bu e-posta adresiyle kayıtlı bir kullanıcı zaten var.");
            }

            string hashliSifre = BCrypt.Net.BCrypt.HashPassword(dto.Sifre);

            var yeniPersonel = new Kullanici
            {
                AdSoyad = dto.AdSoyad,
                Email = dto.Email,
                Telefon = dto.Telefon,
                SifreHash = hashliSifre,
                Rol = atanacakRol
            };

            await _kullaniciRepository.AddAsync(yeniPersonel);
            await _kullaniciRepository.SaveChangesAsync(); 

            if (atanacakRol.Equals("Usta", StringComparison.OrdinalIgnoreCase))
            {
                var yeniUsta = new Usta
                {
                    AdSoyad = yeniPersonel.AdSoyad,
                    KullaniciId = yeniPersonel.Id,
                    UzmanlikAlani = !string.IsNullOrWhiteSpace(dto.UzmanlikAlani) ? dto.UzmanlikAlani : "Genel Usta"
                };

                await _ustaRepository.AddAsync(yeniUsta);
                await _ustaRepository.SaveChangesAsync();
            }

            return (true, $"{atanacakRol} rolündeki personel başarıyla oluşturuldu.");
        }

        public async Task<(bool BasariliMi, string Mesaj)> SifreSifirlamaKoduGonderAsync(SifremiUnuttumDto dto)
        {
            var kullanici = await _kullaniciRepository.GetAsync(k => k.Email == dto.Email && k.SifreHash != null);

            if (kullanici == null)
            {
                return (false, "Bu e-posta adresiyle kayıtlı şifreli bir hesabınız bulunmuyor. Lütfen önce 'Kayıt Ol' sayfasından hesabınızı oluşturun.");
            }

            string resetToken = Guid.NewGuid().ToString();

            kullanici.SifreSifirlamaKodu = resetToken;
            kullanici.SifreSifirlamaKoduBitis = DateTime.Now.AddMinutes(15);

            await _kullaniciRepository.UpdateAsync(kullanici);
            await _kullaniciRepository.SaveChangesAsync();

            // appsettings.json'dan FrontendUrl okunur. Yoksa localhost'a düşer.
            string frontendBaseUrl = _configuration["FrontendUrl"] ?? "http://localhost:5173";
            string frontendSifirlamaLinki = $"{frontendBaseUrl}/sifre-sifirla?token={Uri.EscapeDataString(resetToken)}&email={Uri.EscapeDataString(kullanici.Email)}";

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

            // KESİN GÜVENLİK 1: Token boş gönderilmişse veya DB'deki token boşsa reddet
            if (kullanici == null || 
                string.IsNullOrEmpty(dto.Token) || 
                string.IsNullOrEmpty(kullanici.SifreSifirlamaKodu) || 
                kullanici.SifreSifirlamaKodu != dto.Token)
            {
                return (false, "Geçersiz e-posta adresi veya sıfırlama kodu.");
            }

            // KESİN GÜVENLİK 2: Tarih değeri null ise (.HasValue) veya süre dolmuşsa reddet
            if (!kullanici.SifreSifirlamaKoduBitis.HasValue || kullanici.SifreSifirlamaKoduBitis.Value < DateTime.Now)
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

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, kullanici.Id.ToString()),
                new Claim(ClaimTypes.Role, kullanici.Rol)
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(3),
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

            var emailKontrol = await _kullaniciRepository.GetAsync(k => k.Email == dto.Email && k.Id != kullaniciId);
            if (emailKontrol != null)
                return (false, "Bu e-posta adresi başka bir kullanıcı tarafından kullanılmaktadır.");

            if (!string.IsNullOrEmpty(dto.Telefon))
            {
                var telKontrol = await _kullaniciRepository.GetAsync(k => k.Telefon == dto.Telefon && k.Id != kullaniciId);
                if (telKontrol != null)
                    return (false, "Bu telefon numarası başka bir kullanıcı tarafından kullanılmaktadır.");
            }

            kullanici.AdSoyad = dto.AdSoyad;
            kullanici.Email = dto.Email;
            kullanici.Telefon = dto.Telefon;

            await _kullaniciRepository.UpdateAsync(kullanici);
            await _kullaniciRepository.SaveChangesAsync();

            return (true, "Profil bilgileriniz başarıyla güncellendi.");
        }

        public async Task<(bool BasariliMi, string Mesaj)> SifreGuncelleAsync(int kullaniciId, SifreGuncelleDto dto)
        {
            var kullanici = await _kullaniciRepository.GetAsync(k => k.Id == kullaniciId);
            if (kullanici == null) return (false, "Kullanıcı bulunamadı.");

            if (!BCrypt.Net.BCrypt.Verify(dto.MevcutSifre, kullanici.SifreHash))
            {
                return (false, "Mevcut şifrenizi yanlış girdiniz.");
            }

            kullanici.SifreHash = BCrypt.Net.BCrypt.HashPassword(dto.YeniSifre);
            await _kullaniciRepository.UpdateAsync(kullanici);
            await _kullaniciRepository.SaveChangesAsync();

            return (true, "Şifreniz başarıyla güncellendi.");
        }
    }
}
