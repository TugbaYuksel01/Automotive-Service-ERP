using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OtoServisApi.DTOs;

namespace OtoServerApi.Services
{
    public interface IAiTriageService
    {
        Task<AiAnalizSonucuDto> SikayetiAnalizEtAsync(string sikayetMetni, string plaka);
    }
}
