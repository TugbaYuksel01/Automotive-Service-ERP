namespace OtoServis.Data.Models
{
    public class Usta
    {
        public int Id { get; set; }
        public string AdSoyad { get; set; }
        public string UzmanlikAlani { get; set; }

        // EKSİK OLAN KÖPRÜ BURASIYDI!
        public int? KullaniciId { get; set; }
        public Kullanici Kullanici { get; set; }

        public ICollection<Randevu> Randevular { get; set; }
    }
}