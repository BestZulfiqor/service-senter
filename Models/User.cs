using Microsoft.AspNetCore.Identity;

namespace ServiceCenter.Models
{
    public class User : IdentityUser<int>
    {
        public string FullName { get; set; } = string.Empty;
        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }
        public int? TechnicianId { get; set; }
        public Technician? Technician { get; set; }
    }

    public static class UserRoles
    {
        public const string Admin = "Admin";
        public const string Client = "Client";
        public const string Technician = "Technician";
    }
}
