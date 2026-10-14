namespace OtoServisApi.DTOs
{
    public class KayitliRandevuTalebiDto
    {
        // Plaka her zaman zorunludur (ister listeden seçsin, ister yeni yazsın)
        public string AracPlaka { get; set; }

        // SADECE müşteri "Yeni Araç" seçeneğini kullanırsa arayüzden dolu gelir.
        // Listeden kayıtlı aracını seçerse arayüz buraları null (boş) gönderir.
        public string? AracMarkaModel { get; set; }
        public int? AracUretimYili { get; set; }

        public DateTime Tarih { get; set; }
        public string MusteriNotu { get; set; }
        public string SignalRConnectionId { get; set; }
    }
}
