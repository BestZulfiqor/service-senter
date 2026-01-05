using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using ServiceCenter.Models;
using ServiceCenter.Data;
using ServiceCenter.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ServiceCenter.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReceiptsController : ControllerBase
    {
        private readonly ServiceCenterDbContext _context;
        private readonly UserManager<User> _userManager;

        public ReceiptsController(ServiceCenterDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost("generate/{serviceRequestId}")]
        public async Task<ActionResult<ReceiptDto>> GenerateReceipt(int serviceRequestId)
        {
            var serviceRequest = await _context.ServiceRequests
                .Include(sr => sr.Customer)
                .Include(sr => sr.AssignedTechnician)
                .FirstOrDefaultAsync(sr => sr.Id == serviceRequestId);

            if (serviceRequest == null)
            {
                return NotFound("Service request not found");
            }

            if (serviceRequest.Status != "Завершена")
            {
                return BadRequest("Service request must be completed before generating receipt");
            }

            var existingReceipt = await _context.Receipts
                .FirstOrDefaultAsync(r => r.ServiceRequestId == serviceRequestId);

            if (existingReceipt != null)
            {
                return BadRequest("Receipt already exists for this service request");
            }

            var receiptNumber = $"RCP-{DateTime.Now:yyyyMMdd}-{serviceRequestId:D4}";

            // Автоматически генерируем описание услуги
            var servicesDescription = $"Ремонт {serviceRequest.DeviceBrand} {serviceRequest.DeviceModel}";
            if (!string.IsNullOrEmpty(serviceRequest.ProblemDescription))
            {
                servicesDescription += $" - {serviceRequest.ProblemDescription}";
            }

            var receipt = new Receipt
            {
                ServiceRequestId = serviceRequestId,
                CustomerId = serviceRequest.CustomerId,
                TechnicianId = serviceRequest.AssignedTechnicianId,
                ReceiptNumber = receiptNumber,
                TotalAmount = serviceRequest.FinalCost ?? serviceRequest.EstimatedCost ?? 0,
                ServicesDescription = servicesDescription,
                PaymentMethod = "Наличные",
                Notes = $"Заявка #{serviceRequestId} от {serviceRequest.CreatedAt:dd.MM.yyyy}"
            };

            _context.Receipts.Add(receipt);
            await _context.SaveChangesAsync();

            // Создаем финансовую транзакцию
            var transaction = new FinancialTransaction
            {
                Type = "Income",
                Amount = receipt.TotalAmount,
                Category = "Service",
                Description = $"Оплата услуг по чеку {receipt.ReceiptNumber}",
                ServiceRequestId = serviceRequestId,
                ReceiptId = receipt.Id,
                PaymentMethod = receipt.PaymentMethod,
                CreatedBy = _userManager.GetUserId(User) ?? "System"
            };

            _context.FinancialTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            var receiptDto = new ReceiptDto
            {
                Id = receipt.Id,
                ReceiptNumber = receipt.ReceiptNumber,
                TotalAmount = receipt.TotalAmount,
                ServicesDescription = receipt.ServicesDescription,
                IssuedAt = receipt.IssuedAt,
                IsPaid = receipt.IsPaid,
                PaidAt = receipt.PaidAt,
                PaymentMethod = receipt.PaymentMethod,
                Notes = receipt.Notes,
                ServiceRequestId = receipt.ServiceRequestId,
                CustomerId = receipt.CustomerId,
                TechnicianId = receipt.TechnicianId
            };

            return Ok(receiptDto);
        }

        [HttpGet("{serviceRequestId}")]
        public async Task<ActionResult<Receipt>> GetReceiptByServiceRequest(int serviceRequestId)
        {
            var receipt = await _context.Receipts
                .Include(r => r.Customer)
                .Include(r => r.Technician)
                .Include(r => r.ServiceRequest)
                .FirstOrDefaultAsync(r => r.ServiceRequestId == serviceRequestId);

            if (receipt == null)
            {
                return NotFound();
            }

            return Ok(receipt);
        }

        [HttpPut("{id}/mark-paid")]
        public async Task<ActionResult<Receipt>> MarkAsPaid(int id, [FromBody] MarkPaidDto model)
        {
            var receipt = await _context.Receipts.FindAsync(id);

            if (receipt == null)
            {
                return NotFound();
            }

            receipt.IsPaid = true;
            receipt.PaidAt = DateTime.UtcNow;
            receipt.PaymentMethod = model.PaymentMethod ?? receipt.PaymentMethod;

            await _context.SaveChangesAsync();

            return Ok(receipt);
        }

        [HttpGet("all")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<Receipt>>> GetAllReceipts()
        {
            var receipts = await _context.Receipts
                .Include(r => r.Customer)
                .Include(r => r.Technician)
                .Include(r => r.ServiceRequest)
                .OrderByDescending(r => r.IssuedAt)
                .ToListAsync();

            return Ok(receipts);
        }
    }

    public class MarkPaidDto
    {
        public string PaymentMethod { get; set; } = "";
    }
}
