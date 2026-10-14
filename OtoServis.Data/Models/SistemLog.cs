using System;
using System.ComponentModel.DataAnnotations;

namespace OtoServis.Data.Models // Kendi namespace'ine göre ayarla
{
    public class SistemLog
    {
        [Key]
        public int Id { get; set; }

        public string IslemTipi { get; set; } // EKLEME, GUNCELLEME, SILME, HTTP_GET vb.
        public string TabloAdi { get; set; } // Hangi tabloda işlem yapıldı
        public string Detay { get; set; } // İşlemin detayı
        public DateTime Tarih { get; set; } = DateTime.UtcNow; // İşlem zamanı
        public string KullaniciId { get; set; } // İşlemi yapan kişi veya sistem
    }
}
