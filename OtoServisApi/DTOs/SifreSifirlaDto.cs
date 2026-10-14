using System.ComponentModel.DataAnnotations;

namespace OtoServisApi.DTOs
{
    public class SifreSifirlaDto
    {
        [Required]
        public string Email { get; set; }

        [Required]
        public string Token { get; set; }

        [Required(ErrorMessage = "Yeni şifre zorunludur.")]
        [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
        public string YeniSifre { get; set; }
    }
}
