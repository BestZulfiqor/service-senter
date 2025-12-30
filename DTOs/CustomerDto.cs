using System.ComponentModel.DataAnnotations;

namespace ServiceCenter.DTOs
{
    public class CustomerDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime RegisteredAt { get; set; }
    }

    public class CreateCustomerDto
    {
        [Required(ErrorMessage = "Имя обязательно")]
        [StringLength(200, ErrorMessage = "Имя не может быть длиннее 200 символов")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Телефон обязателен")]
        [Phone(ErrorMessage = "Неверный формат телефона")]
        [StringLength(20, ErrorMessage = "Телефон не может быть длиннее 20 символов")]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Неверный формат email")]
        [StringLength(200, ErrorMessage = "Email не может быть длиннее 200 символов")]
        public string Email { get; set; } = string.Empty;
    }

    public class UpdateCustomerDto
    {
        [Required(ErrorMessage = "Имя обязательно")]
        [StringLength(200, ErrorMessage = "Имя не может быть длиннее 200 символов")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Телефон обязателен")]
        [Phone(ErrorMessage = "Неверный формат телефона")]
        [StringLength(20, ErrorMessage = "Телефон не может быть длиннее 20 символов")]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Неверный формат email")]
        [StringLength(200, ErrorMessage = "Email не может быть длиннее 200 символов")]
        public string Email { get; set; } = string.Empty;
    }
}
