using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OtoServis.Contracts.Events
{
    public enum KaydirmaTipi
    {
        HafifGuncelleme = 1,
        VipErtesiGun = 2,
        KrizErtelemesi = 3
    }

    public class RandevuKaydirildiEvent
    {
        public int RandevuId { get; set; }
        public string MusteriEmail { get; set; } = string.Empty;
        public string MusteriAdSoyad { get; set; } = string.Empty;
        public string AracPlaka { get; set; } = string.Empty;
        public DateTime EskiTarih { get; set; }
        public DateTime YeniTarih { get; set; }
        public KaydirmaTipi Tip { get; set; }
        public string Aciklama { get; set; } = string.Empty;
    }
}
