using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OtoServis.Data.Contracts
{
    public class SifreSifirlamaIstendiEvent
    {
        public string AliciEmail { get; set; }
        public string Konu { get; set; }
        public string HtmlIcerik { get; set; }
    }
}
