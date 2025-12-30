using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceCenter.Data;
using ServiceCenter.Models;
using ServiceCenter.DTOs;
using Swashbuckle.AspNetCore.Annotations;

namespace ServiceCenter.Controllers
{
    /// <summary>
    /// Контроллер для управления техниками
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    [SwaggerTag("Операции для управления техниками сервисного центра")]
    public class TechniciansController : ControllerBase
    {
        private readonly ServiceCenterDbContext _context;

        public TechniciansController(ServiceCenterDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Получить список всех техников
        /// </summary>
        /// <returns>Список техников</returns>
        /// <response code="200">Список техников успешно получен</response>
        [HttpGet]
        [SwaggerOperation(Summary = "Получить всех техников", Description = "Возвращает список всех техников с их заявками")]
        [ProducesResponseType(typeof(IEnumerable<TechnicianDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<TechnicianDto>>> GetTechnicians()
        {
            var technicians = await _context.Technicians
                .Include(t => t.ServiceRequests)
                .ToListAsync();

            var technicianDtos = technicians.Select(t => new TechnicianDto
            {
                Id = t.Id,
                FullName = t.FullName,
                Phone = t.Phone,
                Specialization = t.Specialization
            });

            return Ok(technicianDtos);
        }

        /// <summary>
        /// Получить техника по ID
        /// </summary>
        /// <param name="id">ID техника</param>
        /// <returns>Техник</returns>
        /// <response code="200">Техник найден</response>
        /// <response code="404">Техник не найден</response>
        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Получить техника по ID", Description = "Возвращает информацию о конкретном технике")]
        [ProducesResponseType(typeof(TechnicianDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TechnicianDto>> GetTechnician(int id)
        {
            var technician = await _context.Technicians
                .Include(t => t.ServiceRequests)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (technician == null)
            {
                return NotFound(new { message = $"Техник с ID {id} не найден" });
            }

            var technicianDto = new TechnicianDto
            {
                Id = technician.Id,
                FullName = technician.FullName,
                Phone = technician.Phone,
                Specialization = technician.Specialization
            };

            return Ok(technicianDto);
        }

        /// <summary>
        /// Создать нового техника
        /// </summary>
        /// <param name="technicianDto">Данные техника</param>
        /// <returns>Созданный техник</returns>
        /// <response code="201">Техник успешно создан</response>
        /// <response code="400">Неверные данные</response>
        [HttpPost]
        [SwaggerOperation(Summary = "Создать техника", Description = "Создает нового техника в системе")]
        [ProducesResponseType(typeof(TechnicianDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<TechnicianDto>> PostTechnician([FromBody] CreateTechnicianDto technicianDto)
        {
            var technician = new Technician
            {
                FullName = technicianDto.FullName,
                Phone = technicianDto.Phone,
                Specialization = technicianDto.Specialization
            };

            _context.Technicians.Add(technician);
            await _context.SaveChangesAsync();

            var resultDto = new TechnicianDto
            {
                Id = technician.Id,
                FullName = technician.FullName,
                Phone = technician.Phone,
                Specialization = technician.Specialization
            };

            return CreatedAtAction(nameof(GetTechnician), new { id = technician.Id }, resultDto);
        }

        /// <summary>
        /// Обновить данные техника
        /// </summary>
        /// <param name="id">ID техника</param>
        /// <param name="technicianDto">Обновленные данные техника</param>
        /// <returns>Результат операции</returns>
        /// <response code="204">Техник успешно обновлен</response>
        /// <response code="400">Неверные данные</response>
        /// <response code="404">Техник не найден</response>
        [HttpPut("{id}")]
        [SwaggerOperation(Summary = "Обновить техника", Description = "Обновляет информацию о технике")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PutTechnician(int id, [FromBody] UpdateTechnicianDto technicianDto)
        {
            var technician = await _context.Technicians.FindAsync(id);
            if (technician == null)
            {
                return NotFound(new { message = $"Техник с ID {id} не найден" });
            }

            technician.FullName = technicianDto.FullName;
            technician.Phone = technicianDto.Phone;
            technician.Specialization = technicianDto.Specialization;

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

        /// <summary>
        /// Удалить техника
        /// </summary>
        /// <param name="id">ID техника</param>
        /// <returns>Результат операции</returns>
        /// <response code="204">Техник успешно удален</response>
        /// <response code="404">Техник не найден</response>
        [HttpDelete("{id}")]
        [SwaggerOperation(Summary = "Удалить техника", Description = "Удаляет техника из системы")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteTechnician(int id)
        {
            var technician = await _context.Technicians.FindAsync(id);
            if (technician == null)
            {
                return NotFound(new { message = $"Техник с ID {id} не найден" });
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
