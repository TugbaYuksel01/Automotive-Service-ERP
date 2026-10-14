namespace OtoServis.Data.Models
{
    public class YedekParca
    {
        public int Id { get; set; }
        public string ParcaAdi { get; set; }
        public decimal BirimFiyat { get; set; }
        public int StokMiktari { get; set; }
        public int KritikStokSeviyesi { get; set; } // Örn: 5'in altına düşerse uyarı ver

        // Hangi hizmetlerde bu parça kullanılabilir? (Örn: Sadece Fren Bakımında)
        public int UyumluHizmetTipiId { get; set; }

        // İlişkiler
        public HizmetTipi UyumluHizmetTipi { get; set; }
        public ICollection<RandevuParca> RandevuParcalari { get; set; }
    }
}
