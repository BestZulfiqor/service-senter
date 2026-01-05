using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ServiceCenter.Models;
using ServiceCenter.Data;
using ServiceCenter.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ServiceCenter.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class FinancialController : ControllerBase
    {
        private readonly ServiceCenterDbContext _context;
        private readonly ILogger<FinancialController> _logger;

        public FinancialController(ServiceCenterDbContext context, ILogger<FinancialController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("report")]
        public async Task<ActionResult<FinancialReport>> GetFinancialReport(
            [FromQuery] DateTime startDate, 
            [FromQuery] DateTime endDate)
        {
            try
            {
                startDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
                endDate   = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);
                _logger.LogInformation($"Generating financial report from {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
                
                var transactions = await _context.FinancialTransactions
                    .Include(t => t.ServiceRequest)
                    .Include(t => t.Receipt)
                    .Where(t => t.TransactionDate >= startDate && t.TransactionDate <= endDate)
                    .OrderByDescending(t => t.TransactionDate)
                    .ToListAsync();

                _logger.LogInformation($"Found {transactions.Count} transactions in the specified period");

                var income = transactions.Where(t => t.Type == "Income").Sum(t => t.Amount);
                var expenses = transactions.Where(t => t.Type == "Expense").Sum(t => t.Amount);

                _logger.LogInformation($"Total income: {income}, Total expenses: {expenses}");

                // Защита от деления на ноль
                var incomeForPercentage = income > 0 ? income : 1;
                var expensesForPercentage = expenses > 0 ? expenses : 1;

                var incomeByCategory = transactions
                    .Where(t => t.Type == "Income")
                    .GroupBy(t => t.Category)
                    .Select(g => new FinancialSummary
                    {
                        Category = g.Key,
                        Amount = g.Sum(t => t.Amount),
                        TransactionCount = g.Count(),
                        Percentage = (g.Sum(t => t.Amount) / incomeForPercentage) * 100
                    })
                    .ToList();

                var expensesByCategory = transactions
                    .Where(t => t.Type == "Expense")
                    .GroupBy(t => t.Category)
                    .Select(g => new FinancialSummary
                    {
                        Category = g.Key,
                        Amount = g.Sum(t => t.Amount),
                        TransactionCount = g.Count(),
                        Percentage = (g.Sum(t => t.Amount) / expensesForPercentage) * 100
                    })
                    .ToList();

                var report = new FinancialReport
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    TotalIncome = income,
                    TotalExpenses = expenses,
                    NetProfit = income - expenses,
                    IncomeByCategory = incomeByCategory,
                    ExpensesByCategory = expensesByCategory,
                    Transactions = transactions.Select(t => new TransactionDto
                    {
                        Id = t.Id,
                        Type = t.Type,
                        Amount = t.Amount,
                        Category = t.Category,
                        Description = t.Description,
                        TransactionDate = t.TransactionDate,
                        PaymentMethod = t.PaymentMethod,
                        Notes = t.Notes,
                        CreatedBy = t.CreatedBy,
                        ServiceRequestId = t.ServiceRequestId,
                        ReceiptId = t.ReceiptId
                    }).ToList()
                };

                _logger.LogInformation("Financial report generated successfully");
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating financial report");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("transaction")]
        public async Task<ActionResult<TransactionDto>> AddTransaction([FromBody] AddTransactionDto model)
        {
            try
            {
                var transaction = new FinancialTransaction
                {
                    Type = model.Type,
                    Amount = model.Amount,
                    Category = model.Category,
                    Description = model.Description,
                    PaymentMethod = model.PaymentMethod,
                    Notes = model.Notes ?? string.Empty,
                    CreatedBy = User.Identity?.Name ?? "Admin",
                    TransactionDate = DateTime.UtcNow
                };

                if (model.ServiceRequestId.HasValue)
                {
                    var serviceRequest = await _context.ServiceRequests.FindAsync(model.ServiceRequestId.Value);
                    if (serviceRequest == null)
                    {
                        return BadRequest("Service request not found");
                    }
                    transaction.ServiceRequestId = model.ServiceRequestId.Value;
                }

                _context.FinancialTransactions.Add(transaction);
                await _context.SaveChangesAsync();

                var transactionDto = new TransactionDto
                {
                    Id = transaction.Id,
                    Type = transaction.Type,
                    Amount = transaction.Amount,
                    Category = transaction.Category,
                    Description = transaction.Description,
                    TransactionDate = transaction.TransactionDate,
                    PaymentMethod = transaction.PaymentMethod,
                    Notes = transaction.Notes,
                    CreatedBy = transaction.CreatedBy,
                    ServiceRequestId = transaction.ServiceRequestId,
                    ReceiptId = transaction.ReceiptId
                };

                return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, transactionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding transaction");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("transaction/{id}")]
        public async Task<ActionResult<FinancialTransaction>> GetTransaction(int id)
        {
            var transaction = await _context.FinancialTransactions
                .Include(t => t.ServiceRequest)
                .Include(t => t.Receipt)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction == null)
            {
                return NotFound();
            }

            return Ok(transaction);
        }

        [HttpGet("transactions")]
        public async Task<ActionResult<IEnumerable<TransactionDto>>> GetTransactions(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string? type = null,
            [FromQuery] string? category = null)
        {
            try
            {
                var query = _context.FinancialTransactions.AsQueryable();

                if (startDate.HasValue)
                    query = query.Where(t => t.TransactionDate >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(t => t.TransactionDate <= endDate.Value);

                if (!string.IsNullOrEmpty(type))
                    query = query.Where(t => t.Type == type);

                if (!string.IsNullOrEmpty(category))
                    query = query.Where(t => t.Category == category);

                var transactions = await query
                    .OrderByDescending(t => t.TransactionDate)
                    .ToListAsync();

                var transactionDtos = transactions.Select(t => new TransactionDto
                {
                    Id = t.Id,
                    Type = t.Type,
                    Amount = t.Amount,
                    Category = t.Category,
                    Description = t.Description,
                    TransactionDate = t.TransactionDate,
                    PaymentMethod = t.PaymentMethod,
                    Notes = t.Notes,
                    CreatedBy = t.CreatedBy,
                    ServiceRequestId = t.ServiceRequestId,
                    ReceiptId = t.ReceiptId
                }).ToList();

                return Ok(transactionDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transactions");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("transaction/{id}")]
        public async Task<IActionResult> DeleteTransaction(int id)
        {
            var transaction = await _context.FinancialTransactions.FindAsync(id);
            if (transaction == null)
            {
                return NotFound();
            }

            _context.FinancialTransactions.Remove(transaction);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("summary")]
        public async Task<ActionResult<object>> GetFinancialSummary()
        {
            var currentMonth = DateTime.Now;
            var firstDayOfMonth = new DateTime(currentMonth.Year, currentMonth.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            var currentMonthTransactions = await _context.FinancialTransactions
                .Where(t => t.TransactionDate >= firstDayOfMonth && t.TransactionDate <= lastDayOfMonth)
                .ToListAsync();

            var currentMonthIncome = currentMonthTransactions.Where(t => t.Type == "Income").Sum(t => t.Amount);
            var currentMonthExpenses = currentMonthTransactions.Where(t => t.Type == "Expense").Sum(t => t.Amount);

            var totalIncome = await _context.FinancialTransactions
                .Where(t => t.Type == "Income")
                .SumAsync(t => t.Amount);

            var totalExpenses = await _context.FinancialTransactions
                .Where(t => t.Type == "Expense")
                .SumAsync(t => t.Amount);

            var unpaidReceipts = await _context.Receipts
                .Where(r => !r.IsPaid)
                .SumAsync(r => r.TotalAmount);

            return Ok(new
            {
                CurrentMonthIncome = currentMonthIncome,
                CurrentMonthExpenses = currentMonthExpenses,
                CurrentMonthNetProfit = currentMonthIncome - currentMonthExpenses,
                TotalIncome = totalIncome,
                TotalExpenses = totalExpenses,
                TotalNetProfit = totalIncome - totalExpenses,
                UnpaidReceipts = unpaidReceipts,
                CurrentMonth = currentMonth.ToString("MMMM yyyy")
            });
        }
    }

    public class AddTransactionDto
    {
        public string Type { get; set; } = "";
        public decimal Amount { get; set; }
        public string Category { get; set; } = "";
        public string Description { get; set; } = "";
        public string PaymentMethod { get; set; } = "";
        public string Notes { get; set; } = "";
        public int? ServiceRequestId { get; set; }
    }
}
