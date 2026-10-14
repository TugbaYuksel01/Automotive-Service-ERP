using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OtoServis.Contracts.Events
{
    public class RandevuOlusturulduEvent
    {
        public int RandevuId { get; set; }
        public string MusteriEmail { get; set; } = string.Empty;
        public string MusteriAdSoyad { get; set; } = string.Empty;
        public DateTime RandevuTarihi { get; set; }
        public string AracPlaka { get; set; } = string.Empty;
        public bool IsMisafir { get; set; }
    }
}
