using System.ComponentModel.DataAnnotations;

namespace OtoServisApi.DTOs
{
    public class ProfilGuncelleDto
    {
        public string AdSoyad { get; set; }
        public string Email { get; set; }
        public string? Telefon { get; set; }
        public string? YeniSifre { get; set; } // Opsiyonel
    }
}