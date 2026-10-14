namespace OtoServis.Data.Models
{
    public class RandevuParca
    {
        public int Id { get; set; }
        public int RandevuId { get; set; }
        public int YedekParcaId { get; set; }
        public int KullanilanMiktar { get; set; }

        // İlişkiler
        public Randevu Randevu { get; set; }
        public YedekParca YedekParca { get; set; }
    }
}

