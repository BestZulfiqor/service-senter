using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ServiceCenter.Models;
using ServiceCenter.DTOs;
using ServiceCenter.Data;
using Microsoft.EntityFrameworkCore;

namespace ServiceCenter.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly ServiceCenterDbContext _context;

        public AuthController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IConfiguration configuration,
            ServiceCenterDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _context = context;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto model)
        {
            // Проверяем существование пользователя по email
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                return BadRequest(new { message = "Пользователь с таким email уже существует" });
            }
            
            // Дополнительная проверка по username (тоже email)
            var existingByUsername = await _userManager.FindByNameAsync(model.Email);
            if (existingByUsername != null)
            {
                return BadRequest(new { message = "Пользователь с таким email уже существует" });
            }

            var user = new User
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                PhoneNumber = model.Phone
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                return BadRequest(new { message = "Ошибка создания пользователя", errors = result.Errors });
            }

            // Назначаем роль
            await _userManager.AddToRoleAsync(user, model.Role);

            // Если роль Client, создаем запись в таблице Customers
            if (model.Role == UserRoles.Client)
            {
                var customer = new Customer
                {
                    FullName = model.FullName,
                    Phone = model.Phone,
                    Email = model.Email,
                    RegisteredAt = DateTime.UtcNow
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                user.CustomerId = customer.Id;
                await _userManager.UpdateAsync(user);
            }
            
            // Если роль Technician, создаем запись в таблице Technicians
            if (model.Role == UserRoles.Technician)
            {
                var technician = new Technician
                {
                    FullName = model.FullName,
                    Phone = model.Phone,
                    Specialization = "Общий специалист", // По умолчанию
                    IsActive = true
                };
                _context.Technicians.Add(technician);
                await _context.SaveChangesAsync();

                user.TechnicianId = technician.Id;
                await _userManager.UpdateAsync(user);
            }

            var token = await GenerateJwtToken(user);
            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new AuthResponseDto
            {
                Token = token,
                Email = user.Email!,
                FullName = user.FullName,
                Role = roles.FirstOrDefault() ?? "Client",
                CustomerId = user.CustomerId,
                TechnicianId = user.TechnicianId
            });
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return Unauthorized(new { message = "Неверный email или пароль" });
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);

            if (!result.Succeeded)
            {
                return Unauthorized(new { message = "Неверный email или пароль" });
            }

            var token = await GenerateJwtToken(user);
            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new AuthResponseDto
            {
                Token = token,
                Email = user.Email!,
                FullName = user.FullName,
                Role = roles.FirstOrDefault() ?? "Client",
                CustomerId = user.CustomerId,
                TechnicianId = user.TechnicianId
            });
        }

        [HttpGet("current")]
        public async Task<ActionResult<AuthResponseDto>> GetCurrentUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new AuthResponseDto
            {
                Email = user.Email!,
                FullName = user.FullName,
                Role = roles.FirstOrDefault() ?? "Client",
                CustomerId = user.CustomerId,
                TechnicianId = user.TechnicianId
            });
        }

        private async Task<string> GenerateJwtToken(User user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.Name, user.FullName),
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            if (user.CustomerId.HasValue)
            {
                claims.Add(new Claim("CustomerId", user.CustomerId.Value.ToString()));
            }

            if (user.TechnicianId.HasValue)
            {
                claims.Add(new Claim("TechnicianId", user.TechnicianId.Value.ToString()));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "your-super-secret-key-min-32-characters-long!!!"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"] ?? "ServiceCenter",
                audience: _configuration["Jwt:Audience"] ?? "ServiceCenter",
                claims: claims,
                expires: DateTime.Now.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
