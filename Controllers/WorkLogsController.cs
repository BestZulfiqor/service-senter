using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceCenter.Data;
using ServiceCenter.Models;

namespace ServiceCenter.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WorkLogsController : ControllerBase
    {
        private readonly ServiceCenterDbContext _context;

        public WorkLogsController(ServiceCenterDbContext context)
        {
            _context = context;
        }

        [HttpGet("service-request/{serviceRequestId}")]
        public async Task<ActionResult<IEnumerable<WorkLog>>> GetWorkLogsByServiceRequest(int serviceRequestId)
        {
            return await _context.WorkLogs
                .Where(wl => wl.ServiceRequestId == serviceRequestId)
                .OrderByDescending(wl => wl.LoggedAt)
                .ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<WorkLog>> PostWorkLog(WorkLog workLog)
        {
            _context.WorkLogs.Add(workLog);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetWorkLogsByServiceRequest), 
                new { serviceRequestId = workLog.ServiceRequestId }, workLog);
        }
    }
}
