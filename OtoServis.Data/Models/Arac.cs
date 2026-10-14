namespace OtoServis.Data.Models
{
    public class Arac
    {
        public int Id { get; set; }
        
        public int KullaniciId { get; set; } // Aracın sahibi olan kullanıcının Id'si
        public string Plaka { get; set; }
        public string? MarkaModel { get; set; }
        public int UretimYili { get; set; }

        // Aracın bugüne kadar girdiği tüm servis randevularının geçmişi
        public ICollection<Randevu> GecmisRandevular { get; set; }

        // Navigasyon Özelliği (EF Core bu sayede JOIN işlemlerini otomatik yapar)
        public Kullanici Kullanici { get; set; } // Referans (Navigation Property) değişti!
    }
}
