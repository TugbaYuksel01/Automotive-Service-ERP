using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OtoServis.Data.Models;
using OtoServis.Data.Repositories;

namespace OtoServis.Data.Controllers
{
    
    [Route("api/[controller]")]
    [ApiController]
    public class UstalarController : ControllerBase
    {
        private readonly IRepository<Usta> _ustaRepository;

        public UstalarController(IRepository<Usta> ustaRepository)
        {
            _ustaRepository = ustaRepository;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Usta>>> GetUstalar()
        {
            var ustalar = await _ustaRepository.GetAllAsync();
            return Ok(ustalar);
        }

        [Authorize(Roles = "SuperAdmin, Admin")]
        [HttpPost]
        public async Task<ActionResult<Usta>> PostUsta(Usta usta)
        {
            await _ustaRepository.AddAsync(usta);
            return CreatedAtAction(nameof(GetUstalar), new { id = usta.Id }, usta);
        }
    }
}
