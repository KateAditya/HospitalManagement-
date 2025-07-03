using EyeCareManagement.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace EyeCareManagement.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Patient> Patients { get; set; }
        public DbSet<MedicalRecord> MedicalRecords { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Ward> Wards { get; set; }
        public DbSet<WardAllocation> WardAllocations { get; set; }
        public DbSet<OpticalOrder> OpticalOrders { get; set; }
        public DbSet<Bill> Bills { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Patient relationships
            modelBuilder.Entity<Patient>()
                .HasMany(p => p.MedicalRecords)
                .WithOne(mr => mr.Patient)
                .HasForeignKey(mr => mr.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Patient>()
                .HasMany(p => p.Appointments)
                .WithOne(a => a.Patient)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Patient>()
                .HasMany(p => p.WardAllocations)
                .WithOne(wa => wa.Patient)
                .HasForeignKey(wa => wa.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Patient>()
                .HasMany(p => p.OpticalOrders)
                .WithOne(oo => oo.Patient)
                .HasForeignKey(oo => oo.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Patient>()
                .HasMany(p => p.Bills)
                .WithOne(b => b.Patient)
                .HasForeignKey(b => b.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure ApplicationUser relationships
            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.MedicalRecords)
                .WithOne(mr => mr.Doctor)
                .HasForeignKey(mr => mr.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.Appointments)
                .WithOne(a => a.Doctor)
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.OpticalOrders)
                .WithOne(oo => oo.Doctor)
                .HasForeignKey(oo => oo.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.Bills)
                .WithOne(b => b.GeneratedBy)
                .HasForeignKey(b => b.GeneratedById)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Ward relationships
            modelBuilder.Entity<Ward>()
                .HasMany(w => w.WardAllocations)
                .WithOne(wa => wa.Ward)
                .HasForeignKey(wa => wa.WardId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure OpticalOrder relationships
            modelBuilder.Entity<OpticalOrder>()
                .HasOne(oo => oo.Optician)
                .WithMany()
                .HasForeignKey(oo => oo.OpticianId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure decimal precision
            modelBuilder.Entity<MedicalRecord>()
                .Property(mr => mr.IntraocularPressureLeft)
                .HasPrecision(5, 2);

            modelBuilder.Entity<MedicalRecord>()
                .Property(mr => mr.IntraocularPressureRight)
                .HasPrecision(5, 2);

            modelBuilder.Entity<Appointment>()
                .Property(a => a.ConsultationFee)
                .HasPrecision(8, 2);

            modelBuilder.Entity<OpticalOrder>()
                .Property(oo => oo.EstimatedCost)
                .HasPrecision(8, 2);

            modelBuilder.Entity<OpticalOrder>()
                .Property(oo => oo.FinalCost)
                .HasPrecision(8, 2);

            // Configure Bill decimal properties
            var billEntity = modelBuilder.Entity<Bill>();
            billEntity.Property(b => b.ConsultationCharges).HasPrecision(10, 2);
            billEntity.Property(b => b.ProcedureCharges).HasPrecision(10, 2);
            billEntity.Property(b => b.OpticalCharges).HasPrecision(10, 2);
            billEntity.Property(b => b.WardCharges).HasPrecision(10, 2);
            billEntity.Property(b => b.MedicineCharges).HasPrecision(10, 2);
            billEntity.Property(b => b.OtherCharges).HasPrecision(10, 2);
            billEntity.Property(b => b.Discount).HasPrecision(10, 2);
            billEntity.Property(b => b.TaxAmount).HasPrecision(10, 2);
            billEntity.Property(b => b.TotalAmount).HasPrecision(10, 2);
            billEntity.Property(b => b.PaidAmount).HasPrecision(10, 2);
            billEntity.Property(b => b.BalanceAmount).HasPrecision(10, 2);

            // Configure indexes for better performance
            modelBuilder.Entity<Patient>()
                .HasIndex(p => p.Email)
                .IsUnique();

            modelBuilder.Entity<Patient>()
                .HasIndex(p => p.PhoneNumber);

            modelBuilder.Entity<Appointment>()
                .HasIndex(a => a.AppointmentDate);

            modelBuilder.Entity<Bill>()
                .HasIndex(b => b.BillNumber)
                .IsUnique();

            modelBuilder.Entity<Bill>()
                .HasIndex(b => b.BillDate);
        }
    }
}