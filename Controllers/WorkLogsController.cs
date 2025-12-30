using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceCenter.Data;
using ServiceCenter.Models;
using ServiceCenter.DTOs;
using Swashbuckle.AspNetCore.Annotations;

namespace ServiceCenter.Controllers
{
    /// <summary>
    /// Контроллер для управления журналом работ
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    [SwaggerTag("Операции для управления журналом выполненных работ")]
    public class WorkLogsController : ControllerBase
    {
        private readonly ServiceCenterDbContext _context;

        public WorkLogsController(ServiceCenterDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Получить журнал работ по заявке
        /// </summary>
        /// <param name="serviceRequestId">ID заявки</param>
        /// <returns>Список записей журнала работ</returns>
        /// <response code="200">Журнал работ успешно получен</response>
        [HttpGet("service-request/{serviceRequestId}")]
        [SwaggerOperation(Summary = "Получить журнал работ по заявке", Description = "Возвращает все записи журнала работ для конкретной заявки")]
        [ProducesResponseType(typeof(IEnumerable<WorkLogDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<WorkLogDto>>> GetWorkLogsByServiceRequest(int serviceRequestId)
        {
            var workLogs = await _context.WorkLogs
                .Where(wl => wl.ServiceRequestId == serviceRequestId)
                .OrderByDescending(wl => wl.LoggedAt)
                .ToListAsync();

            var workLogDtos = workLogs.Select(wl => new WorkLogDto
            {
                Id = wl.Id,
                ServiceRequestId = wl.ServiceRequestId,
                Description = wl.Description,
                LoggedBy = wl.LoggedBy,
                LoggedAt = wl.LoggedAt
            });

            return Ok(workLogDtos);
        }

        /// <summary>
        /// Создать новую запись в журнале работ
        /// </summary>
        /// <param name="workLogDto">Данные записи журнала</param>
        /// <returns>Созданная запись</returns>
        /// <response code="201">Запись успешно создана</response>
        /// <response code="400">Неверные данные</response>
        [HttpPost]
        [SwaggerOperation(Summary = "Создать запись в журнале", Description = "Добавляет новую запись в журнал работ")]
        [ProducesResponseType(typeof(WorkLogDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<WorkLogDto>> PostWorkLog([FromBody] CreateWorkLogDto workLogDto)
        {
            var workLog = new WorkLog
            {
                ServiceRequestId = workLogDto.ServiceRequestId,
                Description = workLogDto.Description,
                LoggedBy = workLogDto.LoggedBy,
                LoggedAt = DateTime.UtcNow
            };

            _context.WorkLogs.Add(workLog);
            await _context.SaveChangesAsync();

            var resultDto = new WorkLogDto
            {
                Id = workLog.Id,
                ServiceRequestId = workLog.ServiceRequestId,
                Description = workLog.Description,
                LoggedBy = workLog.LoggedBy,
                LoggedAt = workLog.LoggedAt
            };

            return CreatedAtAction(nameof(GetWorkLogsByServiceRequest), 
                new { serviceRequestId = workLog.ServiceRequestId }, resultDto);
        }
    }
}
