namespace OtoServisApi.DTOs
{
    public class RandevuTamamlamaDto
    {
        public int RandevuId { get; set; }
        public List<KullanilanParcaDto> KullanilanParcalar { get; set; } = new List<KullanilanParcaDto>();
    }
}
