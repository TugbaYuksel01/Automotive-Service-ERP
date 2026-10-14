using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OtoServis.Contracts.Events
{
    public class FaturaKesildiEvent
    {
        public int FaturaId { get; set; }
        public int RandevuId { get; set; }
        public string MusteriEmail { get; set; } = string.Empty;
        public string MusteriAdSoyad { get; set; } = string.Empty;
        public string AracPlaka { get; set; } = string.Empty;
        public decimal ToplamTutar { get; set; }
        public DateTime KesimTarihi { get; set; }
        public string? PdfDosyaYolu { get; set; } // Oluşturulan PDF'in sunucudaki konumu

        public decimal IscilikUcreti { get; set; }
        public decimal Parca { get; set; } //parça idsi


    }
}