using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using ServiceCenter.Models;

namespace ServiceCenter.Data
{
    public class ServiceCenterDbContext : IdentityDbContext<User, Role, int>
    {
        public ServiceCenterDbContext(DbContextOptions<ServiceCenterDbContext> options)
            : base(options)
        {
        }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<ServiceRequest> ServiceRequests { get; set; }
        public DbSet<Technician> Technicians { get; set; }
        public DbSet<WorkLog> WorkLogs { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FullName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Phone).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Email).HasMaxLength(200);
            });

            modelBuilder.Entity<ServiceRequest>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DeviceType).IsRequired().HasMaxLength(100);
                entity.Property(e => e.DeviceBrand).IsRequired().HasMaxLength(100);
                entity.Property(e => e.DeviceModel).IsRequired().HasMaxLength(100);
                entity.Property(e => e.SerialNumber).HasMaxLength(100);
                entity.Property(e => e.ProblemDescription).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.EstimatedCost).HasColumnType("decimal(18,2)");
                entity.Property(e => e.FinalCost).HasColumnType("decimal(18,2)");

                entity.HasOne(e => e.Customer)
                    .WithMany(c => c.ServiceRequests)
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.AssignedTechnician)
                    .WithMany(t => t.ServiceRequests)
                    .HasForeignKey(e => e.AssignedTechnicianId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Technician>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FullName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Phone).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Specialization).IsRequired().HasMaxLength(200);
            });

            modelBuilder.Entity<WorkLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.LoggedBy).IsRequired().HasMaxLength(200);

                entity.HasOne(e => e.ServiceRequest)
                    .WithMany(sr => sr.WorkLogs)
                    .HasForeignKey(e => e.ServiceRequestId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ChatMessage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Message).IsRequired().HasMaxLength(2000);
                entity.Property(e => e.SentAt).IsRequired();
                entity.Property(e => e.IsRead).IsRequired();

                entity.HasOne(e => e.Sender)
                    .WithMany()
                    .HasForeignKey(e => e.SenderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Receiver)
                    .WithMany()
                    .HasForeignKey(e => e.ReceiverId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}