namespace OtoServis.Data.Models
{
    public class Kullanici
    {
        public int Id { get; set; }
        public required string AdSoyad { get; set; }

        // TELEFON ALANI NULLABLE (BOŞ GEÇİLEBİLİR) OLMALIDIR:
        // Çünkü Admin'in telefon girmesi zorunlu değildir, ama Müşteri için gereklidir.
        public string? Telefon { get; set; }
        public string? Email { get; set; }
        public string? SifreHash { get; set; } // Şifreyi açık tutmuyoruz
        public string Rol { get; set; } = "Musteri"; // Varsayılan yetki

        // Şifre sıfırlama işlemleri için eklendi
         public string? SifreSifirlamaKodu { get; set; }
        public DateTime? SifreSifirlamaKoduBitis { get; set; }
    }
}
