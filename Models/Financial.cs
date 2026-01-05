namespace ServiceCenter.Models
{
    public class FinancialTransaction
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty; // "Income", "Expense"
        public decimal Amount { get; set; }
        public string Category { get; set; } = string.Empty; // "Service", "Parts", "Salary", "Rent", "Utilities", etc.
        public string Description { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
        public int? ServiceRequestId { get; set; }
        public ServiceRequest? ServiceRequest { get; set; }
        public int? ReceiptId { get; set; }
        public Receipt? Receipt { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class FinancialReport
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetProfit { get; set; }
        public List<FinancialSummary> IncomeByCategory { get; set; } = new();
        public List<FinancialSummary> ExpensesByCategory { get; set; } = new();
        public List<FinancialTransaction> Transactions { get; set; } = new();
    }

    public class FinancialSummary
    {
        public string Category { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int TransactionCount { get; set; }
        public decimal Percentage { get; set; }
    }
}
