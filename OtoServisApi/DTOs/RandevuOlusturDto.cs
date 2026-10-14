namespace OtoServisApi.DTOs
{
    public class RandevuOlusturDto
    {
        // Müşteri Bilgileri
        public int? MusteriId { get; set; } // Eğer müşteri kayıtlıysa, bu alan dolu olacak
        public string MusteriAdSoyad { get; set; }
        public string MusteriTelefon { get; set; }

        public string Email { get; set; }

        // Araç Bilgileri
        public string AracPlaka { get; set; }
        public string AracMarkaModel { get; set; }
        public int AracUretimYili { get; set; }

        // Randevu Bilgileri
        public int HizmetTipiId { get; set; } // Yapay zekanın seçtiği hizmet
        public DateTime Tarih { get; set; }
        public string MusteriNotu { get; set; }

        public string SignalRConnectionId { get; set; }

        public bool AcilMi { get; set; }
    }
}
