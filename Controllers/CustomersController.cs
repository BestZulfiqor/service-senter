using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceCenter.Data;
using ServiceCenter.Models;
using ServiceCenter.DTOs;
using Swashbuckle.AspNetCore.Annotations;

namespace ServiceCenter.Controllers
{
    /// <summary>
    /// Контроллер для управления клиентами
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    [SwaggerTag("Операции для управления клиентами сервисного центра")]
    public class CustomersController : ControllerBase
    {
        private readonly ServiceCenterDbContext _context;

        public CustomersController(ServiceCenterDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Получить список всех клиентов
        /// </summary>
        /// <returns>Список клиентов</returns>
        /// <response code="200">Список клиентов успешно получен</response>
        [HttpGet]
        [SwaggerOperation(Summary = "Получить всех клиентов", Description = "Возвращает список всех клиентов с их заявками")]
        [ProducesResponseType(typeof(IEnumerable<CustomerDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<CustomerDto>>> GetCustomers()
        {
            var customers = await _context.Customers
                .Include(c => c.ServiceRequests)
                .ToListAsync();

            var customerDtos = customers.Select(c => new CustomerDto
            {
                Id = c.Id,
                FullName = c.FullName,
                Phone = c.Phone,
                Email = c.Email,
                RegisteredAt = c.RegisteredAt
            });

            return Ok(customerDtos);
        }

        /// <summary>
        /// Получить клиента по ID
        /// </summary>
        /// <param name="id">ID клиента</param>
        /// <returns>Клиент</returns>
        /// <response code="200">Клиент найден</response>
        /// <response code="404">Клиент не найден</response>
        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Получить клиента по ID", Description = "Возвращает информацию о конкретном клиенте")]
        [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CustomerDto>> GetCustomer(int id)
        {
            var customer = await _context.Customers
                .Include(c => c.ServiceRequests)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null)
            {
                return NotFound(new { message = $"Клиент с ID {id} не найден" });
            }

            var customerDto = new CustomerDto
            {
                Id = customer.Id,
                FullName = customer.FullName,
                Phone = customer.Phone,
                Email = customer.Email,
                RegisteredAt = customer.RegisteredAt
            };

            return Ok(customerDto);
        }

        /// <summary>
        /// Создать нового клиента
        /// </summary>
        /// <param name="customerDto">Данные клиента</param>
        /// <returns>Созданный клиент</returns>
        /// <response code="201">Клиент успешно создан</response>
        /// <response code="400">Неверные данные</response>
        [HttpPost]
        [SwaggerOperation(Summary = "Создать клиента", Description = "Создает нового клиента в системе")]
        [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<CustomerDto>> PostCustomer([FromBody] CreateCustomerDto customerDto)
        {
            var customer = new Customer
            {
                FullName = customerDto.FullName,
                Phone = customerDto.Phone,
                Email = customerDto.Email,
                RegisteredAt = DateTime.UtcNow
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            var resultDto = new CustomerDto
            {
                Id = customer.Id,
                FullName = customer.FullName,
                Phone = customer.Phone,
                Email = customer.Email,
                RegisteredAt = customer.RegisteredAt
            };

            return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, resultDto);
        }

        /// <summary>
        /// Обновить данные клиента
        /// </summary>
        /// <param name="id">ID клиента</param>
        /// <param name="customerDto">Обновленные данные клиента</param>
        /// <returns>Результат операции</returns>
        /// <response code="204">Клиент успешно обновлен</response>
        /// <response code="400">Неверные данные</response>
        /// <response code="404">Клиент не найден</response>
        [HttpPut("{id}")]
        [SwaggerOperation(Summary = "Обновить клиента", Description = "Обновляет информацию о клиенте")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PutCustomer(int id, [FromBody] UpdateCustomerDto customerDto)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound(new { message = $"Клиент с ID {id} не найден" });
            }

            customer.FullName = customerDto.FullName;
            customer.Phone = customerDto.Phone;
            customer.Email = customerDto.Email;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CustomerExists(id))
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
        /// Удалить клиента
        /// </summary>
        /// <param name="id">ID клиента</param>
        /// <returns>Результат операции</returns>
        /// <response code="204">Клиент успешно удален</response>
        /// <response code="404">Клиент не найден</response>
        [HttpDelete("{id}")]
        [SwaggerOperation(Summary = "Удалить клиента", Description = "Удаляет клиента из системы")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound(new { message = $"Клиент с ID {id} не найден" });
            }

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CustomerExists(int id)
        {
            return _context.Customers.Any(e => e.Id == id);
        }
    }
}
