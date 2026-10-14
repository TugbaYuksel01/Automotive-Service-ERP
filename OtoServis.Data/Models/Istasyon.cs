namespace OtoServis.Data.Models
{
    public class Istasyon
    {
        public int Id { get; set; }
        public required string Ad { get; set; }
        public bool MusaitMi { get; set; }

        // İlişkiler
        public ICollection<Randevu> Randevular { get; set; }
    }
}