namespace OtoServisApi.DTOs
{
    public class RegisterDto
    {
        public required string AdSoyad { get; set; }
        public required string Email { get; set; }
        public required string Sifre { get; set; }
        public string Rol { get; set; } = "Musteri";
    }
}