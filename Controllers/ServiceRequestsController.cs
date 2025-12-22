using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceCenter.Data;
using ServiceCenter.Models;

namespace ServiceCenter.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServiceRequestsController : ControllerBase
    {
        private readonly ServiceCenterDbContext _context;

        public ServiceRequestsController(ServiceCenterDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ServiceRequest>>> GetServiceRequests()
        {
            return await _context.ServiceRequests
                .Include(sr => sr.Customer)
                .Include(sr => sr.AssignedTechnician)
                .Include(sr => sr.WorkLogs)
                .OrderByDescending(sr => sr.CreatedAt)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ServiceRequest>> GetServiceRequest(int id)
        {
            var serviceRequest = await _context.ServiceRequests
                .Include(sr => sr.Customer)
                .Include(sr => sr.AssignedTechnician)
                .Include(sr => sr.WorkLogs)
                .FirstOrDefaultAsync(sr => sr.Id == id);

            if (serviceRequest == null)
            {
                return NotFound();
            }

            return serviceRequest;
        }

        [HttpGet("statistics")]
        public async Task<ActionResult<object>> GetStatistics()
        {
            var totalRequests = await _context.ServiceRequests.CountAsync();
            var newRequests = await _context.ServiceRequests.CountAsync(sr => sr.Status == "Новая");
            var inProgressRequests = await _context.ServiceRequests.CountAsync(sr => sr.Status == "В работе");
            var completedRequests = await _context.ServiceRequests.CountAsync(sr => sr.Status == "Завершена");
            var totalRevenue = await _context.ServiceRequests
                .Where(sr => sr.FinalCost.HasValue)
                .SumAsync(sr => sr.FinalCost.Value);

            return new
            {
                totalRequests,
                newRequests,
                inProgressRequests,
                completedRequests,
                totalRevenue
            };
        }

        [HttpPost]
        public async Task<ActionResult<ServiceRequest>> PostServiceRequest(ServiceRequest serviceRequest)
        {
            _context.ServiceRequests.Add(serviceRequest);
            await _context.SaveChangesAsync();

            var workLog = new WorkLog
            {
                ServiceRequestId = serviceRequest.Id,
                Description = "Заявка создана",
                LoggedBy = "Система",
                LoggedAt = DateTime.UtcNow
            };
            _context.WorkLogs.Add(workLog);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetServiceRequest), new { id = serviceRequest.Id }, serviceRequest);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutServiceRequest(int id, ServiceRequest serviceRequest)
        {
            if (id != serviceRequest.Id)
            {
                return BadRequest();
            }

            var existingRequest = await _context.ServiceRequests
                .AsNoTracking()
                .FirstOrDefaultAsync(sr => sr.Id == id);

            if (existingRequest == null)
            {
                return NotFound();
            }

            _context.Entry(serviceRequest).State = EntityState.Modified;

            if (existingRequest.Status != serviceRequest.Status)
            {
                var workLog = new WorkLog
                {
                    ServiceRequestId = serviceRequest.Id,
                    Description = $"Статус изменен: {existingRequest.Status} → {serviceRequest.Status}",
                    LoggedBy = "Система",
                    LoggedAt = DateTime.UtcNow
                };
                _context.WorkLogs.Add(workLog);
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ServiceRequestExists(id))
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
        public async Task<IActionResult> DeleteServiceRequest(int id)
        {
            var serviceRequest = await _context.ServiceRequests.FindAsync(id);
            if (serviceRequest == null)
            {
                return NotFound();
            }

            _context.ServiceRequests.Remove(serviceRequest);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ServiceRequestExists(int id)
        {
            return _context.ServiceRequests.Any(e => e.Id == id);
        }
    }
}
