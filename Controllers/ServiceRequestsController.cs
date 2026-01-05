using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ServiceCenter.Data;
using ServiceCenter.Models;
using ServiceCenter.DTOs;
using Swashbuckle.AspNetCore.Annotations;

namespace ServiceCenter.Controllers
{
    /// <summary>
    /// Контроллер для управления заявками на ремонт
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    [SwaggerTag("Операции для управления заявками на ремонт")]
    public class ServiceRequestsController : ControllerBase
    {
        private readonly ServiceCenterDbContext _context;

        public ServiceRequestsController(ServiceCenterDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Получить список всех заявок на ремонт
        /// </summary>
        /// <returns>Список заявок</returns>
        /// <response code="200">Список заявок успешно получен</response>
        [HttpGet]
        [SwaggerOperation(Summary = "Получить все заявки", Description = "Возвращает список всех заявок на ремонт с информацией о клиенте и технике")]
        [ProducesResponseType(typeof(IEnumerable<ServiceRequestDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ServiceRequestDto>>> GetServiceRequests()
        {
            var requests = await _context.ServiceRequests
                .Include(sr => sr.Customer)
                .Include(sr => sr.AssignedTechnician)
                .Include(sr => sr.WorkLogs)
                .Include(sr => sr.Receipt)
                .OrderByDescending(sr => sr.CreatedAt)
                .ToListAsync();

            var requestDtos = requests.Select(sr => new ServiceRequestDto
            {
                Id = sr.Id,
                CustomerId = sr.CustomerId,
                CustomerName = sr.Customer.FullName,
                CustomerPhone = sr.Customer.Phone,
                DeviceType = sr.DeviceType,
                DeviceBrand = sr.DeviceBrand,
                DeviceModel = sr.DeviceModel,
                SerialNumber = sr.SerialNumber,
                ProblemDescription = sr.ProblemDescription,
                Status = sr.Status,
                EstimatedCost = sr.EstimatedCost,
                FinalCost = sr.FinalCost,
                CreatedAt = sr.CreatedAt,
                CompletedAt = sr.CompletedAt,
                AssignedTechnicianId = sr.AssignedTechnicianId,
                AssignedTechnicianName = sr.AssignedTechnician?.FullName,
                HasReceipt = sr.Receipt != null,
                Receipt = sr.Receipt != null ? new ReceiptDto
                {
                    Id = sr.Receipt.Id,
                    ReceiptNumber = sr.Receipt.ReceiptNumber,
                    TotalAmount = sr.Receipt.TotalAmount,
                    ServicesDescription = sr.Receipt.ServicesDescription,
                    IssuedAt = sr.Receipt.IssuedAt,
                    IsPaid = sr.Receipt.IsPaid,
                    PaidAt = sr.Receipt.PaidAt,
                    PaymentMethod = sr.Receipt.PaymentMethod,
                    Notes = sr.Receipt.Notes
                } : null
            });

            return Ok(requestDtos);
        }

        /// <summary>
        /// Получить заявку по ID
        /// </summary>
        /// <param name="id">ID заявки</param>
        /// <returns>Заявка на ремонт</returns>
        /// <response code="200">Заявка найдена</response>
        /// <response code="404">Заявка не найдена</response>
        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Получить заявку по ID", Description = "Возвращает детальную информацию о заявке на ремонт")]
        [ProducesResponseType(typeof(ServiceRequestDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ServiceRequestDto>> GetServiceRequest(int id)
        {
            var serviceRequest = await _context.ServiceRequests
                .Include(sr => sr.Customer)
                .Include(sr => sr.AssignedTechnician)
                .Include(sr => sr.WorkLogs)
                .Include(sr => sr.Receipt)
                .FirstOrDefaultAsync(sr => sr.Id == id);

            if (serviceRequest == null)
            {
                return NotFound(new { message = $"Заявка с ID {id} не найдена" });
            }

            var requestDto = new ServiceRequestDto
            {
                Id = serviceRequest.Id,
                CustomerId = serviceRequest.CustomerId,
                CustomerName = serviceRequest.Customer.FullName,
                CustomerPhone = serviceRequest.Customer.Phone,
                DeviceType = serviceRequest.DeviceType,
                DeviceBrand = serviceRequest.DeviceBrand,
                DeviceModel = serviceRequest.DeviceModel,
                SerialNumber = serviceRequest.SerialNumber,
                ProblemDescription = serviceRequest.ProblemDescription,
                Status = serviceRequest.Status,
                EstimatedCost = serviceRequest.EstimatedCost,
                FinalCost = serviceRequest.FinalCost,
                CreatedAt = serviceRequest.CreatedAt,
                CompletedAt = serviceRequest.CompletedAt,
                AssignedTechnicianId = serviceRequest.AssignedTechnicianId,
                AssignedTechnicianName = serviceRequest.AssignedTechnician?.FullName,
                HasReceipt = serviceRequest.Receipt != null,
                Receipt = serviceRequest.Receipt != null ? new ReceiptDto
                {
                    Id = serviceRequest.Receipt.Id,
                    ReceiptNumber = serviceRequest.Receipt.ReceiptNumber,
                    TotalAmount = serviceRequest.Receipt.TotalAmount,
                    ServicesDescription = serviceRequest.Receipt.ServicesDescription,
                    IssuedAt = serviceRequest.Receipt.IssuedAt,
                    IsPaid = serviceRequest.Receipt.IsPaid,
                    PaidAt = serviceRequest.Receipt.PaidAt,
                    PaymentMethod = serviceRequest.Receipt.PaymentMethod,
                    Notes = serviceRequest.Receipt.Notes
                } : null
            };

            return Ok(requestDto);
        }

        /// <summary>
        /// Получить статистику по заявкам
        /// </summary>
        /// <returns>Статистические данные</returns>
        /// <response code="200">Статистика успешно получена</response>
        [HttpGet("statistics")]
        [SwaggerOperation(Summary = "Получить статистику", Description = "Возвращает статистику по заявкам и доходам")]
        [ProducesResponseType(typeof(StatisticsDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<StatisticsDto>> GetStatistics()
        {
            var totalRequests = await _context.ServiceRequests.CountAsync();
            var newRequests = await _context.ServiceRequests.CountAsync(sr => sr.Status == "Новая");
            var inProgressRequests = await _context.ServiceRequests.CountAsync(sr => sr.Status == "В работе");
            var completedRequests = await _context.ServiceRequests.CountAsync(sr => sr.Status == "Завершена");
            var totalRevenue = await _context.ServiceRequests
                .Where(sr => sr.FinalCost.HasValue)
                .SumAsync(sr => sr.FinalCost!.Value);

            var statistics = new StatisticsDto
            {
                TotalRequests = totalRequests,
                NewRequests = newRequests,
                InProgressRequests = inProgressRequests,
                CompletedRequests = completedRequests,
                TotalRevenue = totalRevenue
            };

            return Ok(statistics);
        }

        /// <summary>
        /// Создать новую заявку на ремонт
        /// </summary>
        /// <param name="requestDto">Данные заявки</param>
        /// <returns>Созданная заявка</returns>
        /// <response code="201">Заявка успешно создана</response>
        /// <response code="400">Неверные данные</response>
        [HttpPost]
        [SwaggerOperation(Summary = "Создать заявку", Description = "Создает новую заявку на ремонт")]
        [ProducesResponseType(typeof(ServiceRequestDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ServiceRequestDto>> PostServiceRequest([FromBody] CreateServiceRequestDto requestDto)
        {
            // Проверяем существование клиента
            if (!await _context.Customers.AnyAsync(c => c.Id == requestDto.CustomerId))
            {
                return BadRequest(new { message = "Клиент не найден" });
            }

            // Проверяем существование техника, если он указан
            if (requestDto.AssignedTechnicianId.HasValue && 
                !await _context.Technicians.AnyAsync(t => t.Id == requestDto.AssignedTechnicianId.Value))
            {
                return BadRequest(new { message = "Техник не найден" });
            }

            var serviceRequest = new ServiceRequest
            {
                CustomerId = requestDto.CustomerId,
                DeviceType = requestDto.DeviceType,
                DeviceBrand = requestDto.DeviceBrand,
                DeviceModel = requestDto.DeviceModel,
                SerialNumber = requestDto.SerialNumber,
                ProblemDescription = requestDto.ProblemDescription,
                Status = "Новая",
                EstimatedCost = requestDto.EstimatedCost,
                AssignedTechnicianId = requestDto.AssignedTechnicianId,
                CreatedAt = DateTime.UtcNow
            };

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

            var customer = await _context.Customers.FindAsync(serviceRequest.CustomerId);
            Technician? technician = null;
            if (serviceRequest.AssignedTechnicianId.HasValue)
            {
                technician = await _context.Technicians.FindAsync(serviceRequest.AssignedTechnicianId.Value);
            }

            var resultDto = new ServiceRequestDto
            {
                Id = serviceRequest.Id,
                CustomerId = serviceRequest.CustomerId,
                CustomerName = customer?.FullName ?? string.Empty,
                CustomerPhone = customer?.Phone ?? string.Empty,
                DeviceType = serviceRequest.DeviceType,
                DeviceBrand = serviceRequest.DeviceBrand,
                DeviceModel = serviceRequest.DeviceModel,
                SerialNumber = serviceRequest.SerialNumber,
                ProblemDescription = serviceRequest.ProblemDescription,
                Status = serviceRequest.Status,
                EstimatedCost = serviceRequest.EstimatedCost,
                FinalCost = serviceRequest.FinalCost,
                CreatedAt = serviceRequest.CreatedAt,
                CompletedAt = serviceRequest.CompletedAt,
                AssignedTechnicianId = serviceRequest.AssignedTechnicianId,
                AssignedTechnicianName = technician?.FullName
            };

            return CreatedAtAction(nameof(GetServiceRequest), new { id = serviceRequest.Id }, resultDto);
        }

        /// <summary>
        /// Обновить заявку на ремонт
        /// </summary>
        /// <param name="id">ID заявки</param>
        /// <param name="requestDto">Обновленные данные заявки</param>
        /// <returns>Результат операции</returns>
        /// <response code="204">Заявка успешно обновлена</response>
        /// <response code="400">Неверные данные</response>
        /// <response code="404">Заявка не найдена</response>
        [HttpPut("{id}")]
        [SwaggerOperation(Summary = "Обновить заявку", Description = "Обновляет информацию о заявке на ремонт")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PutServiceRequest(int id, [FromBody] UpdateServiceRequestDto requestDto)
        {
            var serviceRequest = await _context.ServiceRequests.FindAsync(id);
            if (serviceRequest == null)
            {
                return NotFound(new { message = $"Заявка с ID {id} не найдена" });
            }

            var oldStatus = serviceRequest.Status;

            serviceRequest.DeviceType = requestDto.DeviceType;
            serviceRequest.DeviceBrand = requestDto.DeviceBrand;
            serviceRequest.DeviceModel = requestDto.DeviceModel;
            serviceRequest.SerialNumber = requestDto.SerialNumber;
            serviceRequest.ProblemDescription = requestDto.ProblemDescription;
            serviceRequest.Status = requestDto.Status;
            serviceRequest.EstimatedCost = requestDto.EstimatedCost;
            serviceRequest.FinalCost = requestDto.FinalCost;
            serviceRequest.AssignedTechnicianId = requestDto.AssignedTechnicianId;
            serviceRequest.CompletedAt = requestDto.CompletedAt;

            if (oldStatus != serviceRequest.Status)
            {
                var workLog = new WorkLog
                {
                    ServiceRequestId = serviceRequest.Id,
                    Description = $"Статус изменен: {oldStatus} → {serviceRequest.Status}",
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

        /// <summary>
        /// Удалить заявку на ремонт
        /// </summary>
        /// <param name="id">ID заявки</param>
        /// <returns>Результат операции</returns>
        /// <response code="204">Заявка успешно удалена</response>
        /// <response code="404">Заявка не найдена</response>
        [HttpDelete("{id}")]
        [SwaggerOperation(Summary = "Удалить заявку", Description = "Удаляет заявку на ремонт из системы")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteServiceRequest(int id)
        {
            var serviceRequest = await _context.ServiceRequests.FindAsync(id);
            if (serviceRequest == null)
            {
                return NotFound(new { message = $"Заявка с ID {id} не найдена" });
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
