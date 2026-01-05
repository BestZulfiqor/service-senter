using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ServiceCenter.Models;
using ServiceCenter.Data;
using Microsoft.EntityFrameworkCore;

namespace ServiceCenter.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class FinancialController : ControllerBase
    {
        private readonly ServiceCenterDbContext _context;

        public FinancialController(ServiceCenterDbContext context)
        {
            _context = context;
        }

        [HttpGet("report")]
        public async Task<ActionResult<FinancialReport>> GetFinancialReport(
            [FromQuery] DateTime startDate, 
            [FromQuery] DateTime endDate)
        {
            var transactions = await _context.FinancialTransactions
                .Include(t => t.ServiceRequest)
                .Include(t => t.Receipt)
                .Where(t => t.TransactionDate >= startDate && t.TransactionDate <= endDate)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();

            var income = transactions.Where(t => t.Type == "Income").Sum(t => t.Amount);
            var expenses = transactions.Where(t => t.Type == "Expense").Sum(t => t.Amount);

            var incomeByCategory = transactions
                .Where(t => t.Type == "Income")
                .GroupBy(t => t.Category)
                .Select(g => new FinancialSummary
                {
                    Category = g.Key,
                    Amount = g.Sum(t => t.Amount),
                    TransactionCount = g.Count(),
                    Percentage = income > 0 ? (g.Sum(t => t.Amount) / income) * 100 : 0
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
                    Percentage = expenses > 0 ? (g.Sum(t => t.Amount) / expenses) * 100 : 0
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
                Transactions = transactions
            };

            return Ok(report);
        }

        [HttpPost("transaction")]
        public async Task<ActionResult<FinancialTransaction>> AddTransaction([FromBody] AddTransactionDto model)
        {
            var transaction = new FinancialTransaction
            {
                Type = model.Type,
                Amount = model.Amount,
                Category = model.Category,
                Description = model.Description,
                PaymentMethod = model.PaymentMethod,
                Notes = model.Notes,
                CreatedBy = User.Identity?.Name ?? "Admin"
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

            return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, transaction);
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
        public async Task<ActionResult<IEnumerable<FinancialTransaction>>> GetTransactions(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string? type = null,
            [FromQuery] string? category = null)
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
                .Include(t => t.ServiceRequest)
                .Include(t => t.Receipt)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();

            return Ok(transactions);
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
