using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using ServiceCenter.Models;
using ServiceCenter.Data;
using Microsoft.EntityFrameworkCore;

namespace ServiceCenter.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly ServiceCenterDbContext _context;

        public AdminController(
            UserManager<User> userManager,
            RoleManager<Role> roleManager,
            ServiceCenterDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        [HttpGet("users")]
        public async Task<ActionResult<IEnumerable<UserWithRolesDto>>> GetAllUsers()
        {
            var users = await _context.Users
                .Select(u => new UserWithRolesDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    UserName = u.UserName,
                    FullName = u.FullName,
                    PhoneNumber = u.PhoneNumber,
                    CustomerId = u.CustomerId,
                    TechnicianId = u.TechnicianId,
                    Roles = _context.UserRoles
                        .Where(ur => ur.UserId == u.Id)
                        .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                        .ToList()
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpPost("users/{userId}/change-role")]
        public async Task<IActionResult> ChangeUserRole(int userId, [FromBody] ChangeRoleDto model)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return NotFound("User not found");
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            var oldRole = currentRoles.FirstOrDefault() ?? "Client";
            
            // Удаляем все текущие роли
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            // Добавляем новую роль
            var result = await _userManager.AddToRoleAsync(user, model.NewRole);
            if (!result.Succeeded)
            {
                return BadRequest("Failed to assign role");
            }

            // Обрабатываем смену роли и обновляем связи
            if (oldRole == UserRoles.Client && model.NewRole == UserRoles.Technician)
            {
                // Клиент → Техник: удаляем CustomerId, создаем/обновляем TechnicianId
                if (user.CustomerId.HasValue)
                {
                    var customer = await _context.Customers.FindAsync(user.CustomerId.Value);
                    if (customer != null)
                    {
                        _context.Customers.Remove(customer);
                        await _context.SaveChangesAsync();
                    }
                    user.CustomerId = null;
                }

                if (!user.TechnicianId.HasValue)
                {
                    var technician = new Technician
                    {
                        FullName = user.FullName,
                        Phone = user.PhoneNumber ?? "",
                        Specialization = "Общий специалист",
                        IsActive = true
                    };
                    _context.Technicians.Add(technician);
                    await _context.SaveChangesAsync();
                    user.TechnicianId = technician.Id;
                }
            }
            else if (oldRole == UserRoles.Technician && model.NewRole == UserRoles.Client)
            {
                // Техник → Клиент: удаляем TechnicianId, создаем/обновляем CustomerId
                if (user.TechnicianId.HasValue)
                {
                    var technician = await _context.Technicians.FindAsync(user.TechnicianId.Value);
                    if (technician != null)
                    {
                        _context.Technicians.Remove(technician);
                        await _context.SaveChangesAsync();
                    }
                    user.TechnicianId = null;
                }

                if (!user.CustomerId.HasValue)
                {
                    var customer = new Customer
                    {
                        FullName = user.FullName,
                        Phone = user.PhoneNumber ?? "",
                        Email = user.Email ?? "",
                        RegisteredAt = DateTime.UtcNow
                    };
                    _context.Customers.Add(customer);
                    await _context.SaveChangesAsync();
                    user.CustomerId = customer.Id;
                }
            }
            else if (model.NewRole == UserRoles.Admin)
            {
                // Любая роль → Admin: удаляем связи с клиентом/техником
                if (user.CustomerId.HasValue)
                {
                    var customer = await _context.Customers.FindAsync(user.CustomerId.Value);
                    if (customer != null)
                    {
                        _context.Customers.Remove(customer);
                        await _context.SaveChangesAsync();
                    }
                    user.CustomerId = null;
                }

                if (user.TechnicianId.HasValue)
                {
                    var technician = await _context.Technicians.FindAsync(user.TechnicianId.Value);
                    if (technician != null)
                    {
                        _context.Technicians.Remove(technician);
                        await _context.SaveChangesAsync();
                    }
                    user.TechnicianId = null;
                }
            }
            else
            {
                // Для других случаев (Admin → Client/Technician)
                if (model.NewRole == UserRoles.Client && !user.CustomerId.HasValue)
                {
                    var customer = new Customer
                    {
                        FullName = user.FullName,
                        Phone = user.PhoneNumber ?? "",
                        Email = user.Email ?? "",
                        RegisteredAt = DateTime.UtcNow
                    };
                    _context.Customers.Add(customer);
                    await _context.SaveChangesAsync();
                    user.CustomerId = customer.Id;
                }
                else if (model.NewRole == UserRoles.Technician && !user.TechnicianId.HasValue)
                {
                    var technician = new Technician
                    {
                        FullName = user.FullName,
                        Phone = user.PhoneNumber ?? "",
                        Specialization = "Общий специалист",
                        IsActive = true
                    };
                    _context.Technicians.Add(technician);
                    await _context.SaveChangesAsync();
                    user.TechnicianId = technician.Id;
                }
            }

            await _userManager.UpdateAsync(user);

            return Ok(new { message = $"User role changed to {model.NewRole}" });
        }

        [HttpDelete("users/{userId}")]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return NotFound("User not found");
            }

            // Проверяем, что это не админ
            var isAdmin = await _userManager.IsInRoleAsync(user, UserRoles.Admin);
            if (isAdmin)
            {
                return BadRequest("Cannot delete admin user");
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest("Failed to delete user");
            }

            return Ok(new { message = "User deleted successfully" });
        }

        [HttpGet("roles")]
        public async Task<ActionResult<IEnumerable<string>>> GetAllRoles()
        {
            var roles = await _roleManager.Roles
                .Select(r => r.Name)
                .ToListAsync();

            return Ok(roles);
        }
    }

    public class UserWithRolesDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = "";
        public string UserName { get; set; } = "";
        public string FullName { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public int? CustomerId { get; set; }
        public int? TechnicianId { get; set; }
        public List<string> Roles { get; set; } = new();
    }

    public class ChangeRoleDto
    {
        public string NewRole { get; set; } = "";
    }
}
