namespace OtoServisApi.DTOs
{
    public class RandevuDetayDto
    {
        public int RandevuNo { get; set; }
        public string Tarih { get; set; }
        public string MusteriBilgisi { get; set; }
        public string AracPlaka { get; set; }
        public string HizmetAdi { get; set; }
        public decimal ToplamTutar { get; set; }
        public string SorumluUsta { get; set; }
    }
}
