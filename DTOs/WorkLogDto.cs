using System.ComponentModel.DataAnnotations;

namespace ServiceCenter.DTOs
{
    public class WorkLogDto
    {
        public int Id { get; set; }
        public int ServiceRequestId { get; set; }
        public string Description { get; set; } = string.Empty;
        public string LoggedBy { get; set; } = string.Empty;
        public DateTime LoggedAt { get; set; }
    }

    public class CreateWorkLogDto
    {
        [Required(ErrorMessage = "ID заявки обязателен")]
        public int ServiceRequestId { get; set; }

        [Required(ErrorMessage = "Описание обязательно")]
        [StringLength(1000, ErrorMessage = "Описание не может быть длиннее 1000 символов")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Имя автора обязательно")]
        [StringLength(200, ErrorMessage = "Имя автора не может быть длиннее 200 символов")]
        public string LoggedBy { get; set; } = string.Empty;
    }
}
