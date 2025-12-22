using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceCenter.Data;
using ServiceCenter.Models;

namespace ServiceCenter.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TechniciansController : ControllerBase
    {
        private readonly ServiceCenterDbContext _context;

        public TechniciansController(ServiceCenterDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Technician>>> GetTechnicians()
        {
            return await _context.Technicians
                .Include(t => t.ServiceRequests)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Technician>> GetTechnician(int id)
        {
            var technician = await _context.Technicians
                .Include(t => t.ServiceRequests)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (technician == null)
            {
                return NotFound();
            }

            return technician;
        }

        [HttpPost]
        public async Task<ActionResult<Technician>> PostTechnician(Technician technician)
        {
            _context.Technicians.Add(technician);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTechnician), new { id = technician.Id }, technician);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutTechnician(int id, Technician technician)
        {
            if (id != technician.Id)
            {
                return BadRequest();
            }

            _context.Entry(technician).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TechnicianExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTechnician(int id)
        {
            var technician = await _context.Technicians.FindAsync(id);
            if (technician == null)
            {
                return NotFound();
            }

            _context.Technicians.Remove(technician);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TechnicianExists(int id)
        {
            return _context.Technicians.Any(e => e.Id == id);
        }
    }
}
