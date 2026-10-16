using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OtoServis.Data.Models;
using OtoServis.Data.Repositories;

namespace OtoServisApi.Controllers
{
    [EnableRateLimiting("SabitZamanliKural")]
    [Route("api/[controller]")]
    [ApiController]
    public class IstasyonlarController : ControllerBase
    {
        private readonly IRepository<Istasyon> _istasyonRepository;

        public IstasyonlarController(IRepository<Istasyon> istasyonRepository)
        {
            _istasyonRepository = istasyonRepository;
        }

        [HttpGet]
        [Authorize(Roles = "SuperAdmin, Admin, Usta")]
        public async Task<ActionResult<IEnumerable<Istasyon>>> GetIstasyonlar()
        {
            var istasyonlar = await _istasyonRepository.GetAllAsync();
            return Ok(istasyonlar);
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin, Admin")]
        public async Task<ActionResult<Istasyon>> PostIstasyon(Istasyon istasyon)
        {
            await _istasyonRepository.AddAsync(istasyon);
            return CreatedAtAction(nameof(GetIstasyonlar), new { id = istasyon.Id }, istasyon);
        }
    }
}
