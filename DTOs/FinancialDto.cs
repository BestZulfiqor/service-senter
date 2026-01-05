using System.ComponentModel.DataAnnotations;

namespace ServiceCenter.DTOs
{
    public class AddTransactionDto
    {
        [Required(ErrorMessage = "Тип обязателен")]
        public string Type { get; set; } = string.Empty;

        [Required(ErrorMessage = "Сумма обязательна")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Сумма должна быть больше 0")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Категория обязательна")]
        public string Category { get; set; } = string.Empty;

        [Required(ErrorMessage = "Описание обязательно")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Способ оплаты обязателен")]
        public string PaymentMethod { get; set; } = string.Empty;

        public string? Notes { get; set; }
        public int? ServiceRequestId { get; set; }
    }

    public class TransactionDto
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public int? ServiceRequestId { get; set; }
        public int? ReceiptId { get; set; }
    }
}
