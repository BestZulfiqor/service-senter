using System.ComponentModel.DataAnnotations;

namespace ServiceCenter.DTOs
{
    public class TechnicianDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
    }

    public class CreateTechnicianDto
    {
        [Required(ErrorMessage = "Имя обязательно")]
        [StringLength(200, ErrorMessage = "Имя не может быть длиннее 200 символов")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Телефон обязателен")]
        [Phone(ErrorMessage = "Неверный формат телефона")]
        [StringLength(20, ErrorMessage = "Телефон не может быть длиннее 20 символов")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Специализация обязательна")]
        [StringLength(200, ErrorMessage = "Специализация не может быть длиннее 200 символов")]
        public string Specialization { get; set; } = string.Empty;
    }

    public class UpdateTechnicianDto
    {
        [Required(ErrorMessage = "Имя обязательно")]
        [StringLength(200, ErrorMessage = "Имя не может быть длиннее 200 символов")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Телефон обязателен")]
        [Phone(ErrorMessage = "Неверный формат телефона")]
        [StringLength(20, ErrorMessage = "Телефон не может быть длиннее 20 символов")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Специализация обязательна")]
        [StringLength(200, ErrorMessage = "Специализация не может быть длиннее 200 символов")]
        public string Specialization { get; set; } = string.Empty;
    }
}
