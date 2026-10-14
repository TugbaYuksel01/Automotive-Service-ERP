namespace OtoServisApi.DTOs
{
    public class AiTalepDto
    {
        public string MusteriSikayeti { get; set; }

        // AI'ın geçmişi bulabilmesi için eklendi
        public string Plaka { get; set; }

        // Müşterinin tarayıcısından (Frontend'den) HTTP POST ile gelecek olan eşsiz ID
        public string SignalRConnectionId { get; set; }
    }
}
