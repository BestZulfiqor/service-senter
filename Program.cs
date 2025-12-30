using System.Reflection;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using ServiceCenter.Data;
using ServiceCenter.Models;
using FluentValidation.AspNetCore;
using Swashbuckle.AspNetCore.Annotations;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Service Center API",
        Description = "API для управления сервисным центром по ремонту техники",
        Contact = new OpenApiContact
        {
            Name = "Service Center Support",
            Email = "support@servicecenter.com"
        },
        License = new OpenApiLicense
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    options.EnableAnnotations();
    
    options.TagActionsBy(api =>
    {
        if (api.GroupName != null)
        {
            return new[] { api.GroupName };
        }

        var controllerActionDescriptor = api.ActionDescriptor as Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor;
        if (controllerActionDescriptor != null)
        {
            return new[] { controllerActionDescriptor.ControllerName };
        }

        throw new InvalidOperationException("Unable to determine tag for endpoint.");
    });

    options.DocInclusionPredicate((name, api) => true);
});

builder.Services.AddFluentValidationAutoValidation()
    .AddFluentValidationClientsideAdapters();

builder.Services.AddDbContext<ServiceCenterDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Настройка Identity
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ServiceCenterDbContext>()
.AddDefaultTokenProviders();

// Настройка JWT
var jwtKey = builder.Configuration["Jwt:Key"] ?? "your-super-secret-key-min-32-characters-long!!!";
var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "ServiceCenter",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "ServiceCenter",
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Service Center API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "Service Center API Documentation";
        options.DisplayRequestDuration();
        options.EnableDeepLinking();
        options.EnableFilter();
        options.ShowExtensions();
        options.EnableValidator();
    });

// Инициализация ролей
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var roles = new[] { UserRoles.Admin, UserRoles.Client, UserRoles.Technician };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}

app.UseCors("AllowAll");
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapFallbackToFile("index.html");

app.Run();
