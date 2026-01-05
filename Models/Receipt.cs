namespace ServiceCenter.Models
{
    public class Receipt
    {
        public int Id { get; set; }
        public int ServiceRequestId { get; set; }
        public ServiceRequest ServiceRequest { get; set; } = null!;
        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;
        public int? TechnicianId { get; set; }
        public Technician? Technician { get; set; }
        public string ReceiptNumber { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string ServicesDescription { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
        public bool IsPaid { get; set; } = false;
        public DateTime? PaidAt { get; set; }
        public string PaymentMethod { get; set; } = string.Empty; // "Наличные", "Карта", "Перевод"
        public string Notes { get; set; } = string.Empty;
    }
}
