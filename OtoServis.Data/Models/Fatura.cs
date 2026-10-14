namespace OtoServis.Data.Models
{
    public class Fatura
    {
        public int Id { get; set; }
        public int RandevuId { get; set; }
        public decimal ToplamParcaMaliyeti { get; set; }
        public decimal IscilikUcreti { get; set; }
        public decimal GenelToplam => ToplamParcaMaliyeti + IscilikUcreti;
        public DateTime KesimTarihi { get; set; }
        public string? PdfDosyaYolu { get; set; } // Oluşturulan PDF'in sunucudaki konumu

        public bool OdendiMi { get; set; } = false;
        public DateTime? OdemeTarihi { get; set; }
        public string? OdemeYontemi { get; set; } // "Nakit", "POS", "OnlineKrediKarti"
        public int? TahsilEdenKullaniciId { get; set; } // Veznedarın Id'si

        // İlişkiler (Bire Bir)
        public Randevu Randevu { get; set; }
    }
}
