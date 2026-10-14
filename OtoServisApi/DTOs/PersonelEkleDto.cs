using System.ComponentModel.DataAnnotations;

namespace OtoServisApi.DTOs
{
    public class PersonelEkleDto
    {
        [Required(ErrorMessage = "Ad Soyad alanı zorunludur.")]
        [StringLength(100)]
        public string AdSoyad { get; set; }

        [Required(ErrorMessage = "E-posta alanı zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        public string Email { get; set; }

        public string? Telefon { get; set; }

        [Required(ErrorMessage = "Şifre alanı zorunludur.")]
        [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
        public string Sifre { get; set; }

        [Required(ErrorMessage = "Rol alanı zorunludur.")]
        public string Rol { get; set; } // Sadece "Admin" veya "Usta" kabul edilecek

        public string? UzmanlikAlani { get; set; }
    }
}
