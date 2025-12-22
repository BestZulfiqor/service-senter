namespace ServiceCenter.Models
{
    public class ServiceRequest
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;
        public string DeviceType { get; set; } = string.Empty;
        public string DeviceBrand { get; set; } = string.Empty;
        public string DeviceModel { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string ProblemDescription { get; set; } = string.Empty;
        public string Status { get; set; } = "Новая";
        public decimal? EstimatedCost { get; set; }
        public decimal? FinalCost { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public int? AssignedTechnicianId { get; set; }
        public Technician? AssignedTechnician { get; set; }
        public ICollection<WorkLog> WorkLogs { get; set; } = new List<WorkLog>();
    }
}
