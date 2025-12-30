using System.ComponentModel.DataAnnotations;

namespace ServiceCenter.DTOs
{
    public class ServiceRequestDto
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string DeviceType { get; set; } = string.Empty;
        public string DeviceBrand { get; set; } = string.Empty;
        public string DeviceModel { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string ProblemDescription { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal? EstimatedCost { get; set; }
        public decimal? FinalCost { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int? AssignedTechnicianId { get; set; }
        public string? AssignedTechnicianName { get; set; }
    }

    public class CreateServiceRequestDto
    {
        [Required(ErrorMessage = "ID клиента обязателен")]
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "Тип устройства обязателен")]
        [StringLength(100, ErrorMessage = "Тип устройства не может быть длиннее 100 символов")]
        public string DeviceType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Бренд устройства обязателен")]
        [StringLength(100, ErrorMessage = "Бренд устройства не может быть длиннее 100 символов")]
        public string DeviceBrand { get; set; } = string.Empty;

        [Required(ErrorMessage = "Модель устройства обязательна")]
        [StringLength(100, ErrorMessage = "Модель устройства не может быть длиннее 100 символов")]
        public string DeviceModel { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Серийный номер не может быть длиннее 100 символов")]
        public string SerialNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Описание проблемы обязательно")]
        [StringLength(1000, ErrorMessage = "Описание проблемы не может быть длиннее 1000 символов")]
        public string ProblemDescription { get; set; } = string.Empty;

        [Range(0, double.MaxValue, ErrorMessage = "Предполагаемая стоимость должна быть положительной")]
        public decimal? EstimatedCost { get; set; }

        public int? AssignedTechnicianId { get; set; }
    }

    public class UpdateServiceRequestDto
    {
        [Required(ErrorMessage = "Тип устройства обязателен")]
        [StringLength(100, ErrorMessage = "Тип устройства не может быть длиннее 100 символов")]
        public string DeviceType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Бренд устройства обязателен")]
        [StringLength(100, ErrorMessage = "Бренд устройства не может быть длиннее 100 символов")]
        public string DeviceBrand { get; set; } = string.Empty;

        [Required(ErrorMessage = "Модель устройства обязательна")]
        [StringLength(100, ErrorMessage = "Модель устройства не может быть длиннее 100 символов")]
        public string DeviceModel { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Серийный номер не может быть длиннее 100 символов")]
        public string SerialNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Описание проблемы обязательно")]
        [StringLength(1000, ErrorMessage = "Описание проблемы не может быть длиннее 1000 символов")]
        public string ProblemDescription { get; set; } = string.Empty;

        [Required(ErrorMessage = "Статус обязателен")]
        [StringLength(50, ErrorMessage = "Статус не может быть длиннее 50 символов")]
        public string Status { get; set; } = string.Empty;

        [Range(0, double.MaxValue, ErrorMessage = "Предполагаемая стоимость должна быть положительной")]
        public decimal? EstimatedCost { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Итоговая стоимость должна быть положительной")]
        public decimal? FinalCost { get; set; }

        public int? AssignedTechnicianId { get; set; }
        
        public DateTime? CompletedAt { get; set; }
    }

    public class StatisticsDto
    {
        public int TotalRequests { get; set; }
        public int NewRequests { get; set; }
        public int InProgressRequests { get; set; }
        public int CompletedRequests { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
