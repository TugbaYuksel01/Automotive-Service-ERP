using System.ComponentModel.DataAnnotations;

namespace OtoServisApi.DTOs
{
    public class SifreGuncelleDto
    {
        [Required(ErrorMessage = "Mevcut şifre zorunludur.")]
        public string MevcutSifre { get; set; }

        [Required(ErrorMessage = "Yeni şifre zorunludur.")]
        [MinLength(6, ErrorMessage = "Yeni şifre en az 6 karakter olmalıdır.")]
        public string YeniSifre { get; set; }
    }
}