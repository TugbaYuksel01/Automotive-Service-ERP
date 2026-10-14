namespace OtoServis.Data.Models
{
    
    public class HizmetTipi
    {
        public int Id { get; set; }
        public required string Ad { get; set; }
        public decimal Fiyat { get; set; }
        public int UstaSureDakika { get; set; }
        public int LiftSureDakika { get; set; }
        public string GerekenUzmanlik { get; set; }

        // İlişkiler
        public ICollection<Randevu> Randevular { get; set; }
        public ICollection<YedekParca> UyumluParcalar { get; set; }
    }
}