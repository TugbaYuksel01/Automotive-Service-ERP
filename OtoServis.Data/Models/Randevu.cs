using OtoServis.Data.Models;

namespace  OtoServis.Data.Models;

public class Randevu
{
    public int Id { get; set; }
    public int? AracId { get; set; }
    public int KullaniciId { get; set; } // FK alanı zorunlu olarak kalıyor
    public int HizmetTipiId { get; set; }
    public int? UstaId { get; set; }
    public int? IstasyonId { get; set; }


    public DateTime Tarih { get; set; }
    public string? MusteriNotu { get; set; }
    public bool AcilMi { get; set; }
    public RandevuDurumu Durum { get; set; } 
    public DateTime? GerceklesenBaslangic { get; set; }
    public DateTime? GerceklesenBitis { get; set; }


    // İlişkiler
    public Arac Arac { get; set; }
    public Kullanici Kullanici { get; set; } // Referans (Navigation Property) değişti!
    public HizmetTipi? HizmetTipi { get; set; }
    public Usta? Usta { get; set; }
    public Istasyon? Istasyon { get; set; }
    public Fatura Fatura { get; set; }

    public DateTime? TeslimTarihi { get; set; } // Null ise araç dükkanda bekliyor
    public int? TeslimEdenKullaniciId { get; set; } // Anahtarı müşteriye veren veznedar
    public ICollection<RandevuParca> RandevuParcalari { get; set; }
}


