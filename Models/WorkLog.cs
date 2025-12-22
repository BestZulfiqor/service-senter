namespace ServiceCenter.Models
{
    public class WorkLog
    {
        public int Id { get; set; }
        public int ServiceRequestId { get; set; }
        public ServiceRequest ServiceRequest { get; set; } = null!;
        public string Description { get; set; } = string.Empty;
        public DateTime LoggedAt { get; set; } = DateTime.UtcNow;
        public string LoggedBy { get; set; } = string.Empty;
    }
}
