using System;
using System.Collections.Generic;
using HospitalManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Data;

public partial class HospitalManagementContext : DbContext
{
    public HospitalManagementContext()
    {
    }

    public HospitalManagementContext(DbContextOptions<HospitalManagementContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Appointment> Appointments { get; set; }

    public virtual DbSet<Doctor> Doctors { get; set; }

    public virtual DbSet<Feedback> Feedbacks { get; set; }

    public virtual DbSet<News> News { get; set; }

    public virtual DbSet<Package> Packages { get; set; }

    public virtual DbSet<PackageCategory> PackageCategories { get; set; }

    public virtual DbSet<PackageTest> PackageTests { get; set; }

    public virtual DbSet<PasswordReset> PasswordResets { get; set; }

    public virtual DbSet<Patient> Patients { get; set; }

    public virtual DbSet<Room> Rooms { get; set; }

    public virtual DbSet<Schedule> Schedules { get; set; }

    public virtual DbSet<Service> Services { get; set; }

    public virtual DbSet<Slot> Slots { get; set; }

    public virtual DbSet<Staff> Staff { get; set; }

    public virtual DbSet<Test> Tests { get; set; }

    public virtual DbSet<TestList> TestLists { get; set; }

    public virtual DbSet<Tracking> Trackings { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=localhost;Database=Hospital_Management;User ID=sa;Password=123;Encrypt=False;TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasKey(e => e.AppointmentId).HasName("PK__Appointm__8ECDFCA20901C289");

            entity.ToTable("Appointment");

            entity.HasIndex(e => e.AppointmentCode, "UQ__Appointm__F67FE26FF5BE6C86").IsUnique();

            entity.Property(e => e.AppointmentId).HasColumnName("AppointmentID");
            entity.Property(e => e.AppointmentCode).HasMaxLength(20);
            entity.Property(e => e.Diagnosis).HasMaxLength(100);
            entity.Property(e => e.DoctorId).HasColumnName("DoctorID");
            entity.Property(e => e.Note).HasMaxLength(255);
            entity.Property(e => e.PackageId).HasColumnName("PackageID");
            entity.Property(e => e.PatientId).HasColumnName("PatientID");
            entity.Property(e => e.PaymentMethod).HasMaxLength(20);
            entity.Property(e => e.PaymentStatus).HasMaxLength(20);
            entity.Property(e => e.RecordCreatedAt).HasColumnType("datetime");
            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");
            entity.Property(e => e.SlotId).HasColumnName("SlotID");
            entity.Property(e => e.StaffId).HasColumnName("StaffID");
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.Symptoms).HasMaxLength(100);
            entity.Property(e => e.TotalPrice).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.TransactionCode).HasMaxLength(100);

            entity.HasOne(d => d.Doctor).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.DoctorId)
                .HasConstraintName("FK__Appointme__Docto__571DF1D5");

            entity.HasOne(d => d.Package).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.PackageId)
                .HasConstraintName("FK__Appointme__Packa__59FA5E80");

            entity.HasOne(d => d.Patient).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Appointme__Patie__5812160E");

            entity.HasOne(d => d.Service).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.ServiceId)
                .HasConstraintName("FK__Appointme__Servi__59063A47");

            entity.HasOne(d => d.Slot).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.SlotId)
                .HasConstraintName("FK__Appointme__SlotI__5BE2A6F2");

            entity.HasOne(d => d.Staff).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.StaffId)
                .HasConstraintName("FK__Appointme__Staff__5AEE82B9");
        });

        modelBuilder.Entity<Doctor>(entity =>
        {
            entity.HasKey(e => e.DoctorId).HasName("PK__Doctor__2DC00EDFF5582C88");

            entity.ToTable("Doctor");

            entity.HasIndex(e => e.Email, "UQ__Doctor__A9D105340BC75F13").IsUnique();

            entity.Property(e => e.DoctorId).HasColumnName("DoctorID");
            entity.Property(e => e.Degree).HasMaxLength(20);
            entity.Property(e => e.DepartmentName).HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.Gender).HasMaxLength(1);
            entity.Property(e => e.IsActive).HasColumnName("isActive");
            entity.Property(e => e.IsDepartmentHead).HasColumnName("isDepartmentHead");
            entity.Property(e => e.IsSpecial).HasColumnName("isSpecial");
            entity.Property(e => e.PasswordHash).HasMaxLength(555);
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.ProfileImage).IsUnicode(false);
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(e => e.FeedbackId).HasName("PK__Feedback__6A4BEDF600C9322A");

            entity.ToTable("Feedback");

            entity.Property(e => e.FeedbackId).HasColumnName("FeedbackID");
            entity.Property(e => e.Comment).HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.PatientId).HasColumnName("PatientID");
            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");

            entity.HasOne(d => d.Patient).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Feedback__Patien__628FA481");

            entity.HasOne(d => d.Service).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Feedback__Servic__6383C8BA");
        });

        modelBuilder.Entity<News>(entity =>
        {
            entity.HasKey(e => e.NewsId).HasName("PK__News__954EBDD3B27BD7BC");

            entity.Property(e => e.NewsId).HasColumnName("NewsID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.DoctorId).HasColumnName("DoctorID");
            entity.Property(e => e.StaffId).HasColumnName("StaffID");
            entity.Property(e => e.Title).HasMaxLength(255);

            entity.HasOne(d => d.Doctor).WithMany(p => p.News)
                .HasForeignKey(d => d.DoctorId)
                .HasConstraintName("FK__News__DoctorID__6E01572D");

            entity.HasOne(d => d.Staff).WithMany(p => p.News)
                .HasForeignKey(d => d.StaffId)
                .HasConstraintName("FK__News__StaffID__6D0D32F4");
        });

        modelBuilder.Entity<Package>(entity =>
        {
            entity.HasKey(e => e.PackageId).HasName("PK__Package__322035EC7CBC528C");

            entity.ToTable("Package");

            entity.Property(e => e.PackageId).HasColumnName("PackageID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.DiscountPercent).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.FinalPrice).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.OriginalPrice).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.PackageCategoryId).HasColumnName("PackageCategoryID");
            entity.Property(e => e.PackageName).HasMaxLength(100);
            entity.Property(e => e.TargetGender).HasMaxLength(1);
            entity.Property(e => e.Thumbnail).IsUnicode(false);

            entity.HasOne(d => d.PackageCategory).WithMany(p => p.Packages)
                .HasForeignKey(d => d.PackageCategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Package__Package__4D94879B");
        });

        modelBuilder.Entity<PackageCategory>(entity =>
        {
            entity.HasKey(e => e.PackageCategoryId).HasName("PK__PackageC__BAB8ED7E177941FC");

            entity.ToTable("PackageCategory");

            entity.Property(e => e.PackageCategoryId).HasColumnName("PackageCategoryID");
            entity.Property(e => e.CategoryName).HasMaxLength(100);
        });

        modelBuilder.Entity<PackageTest>(entity =>
        {
            entity.HasKey(e => e.PackageTestId).HasName("PK__PackageT__DB90EDCE69361473");

            entity.ToTable("PackageTest");

            entity.Property(e => e.PackageTestId).HasColumnName("PackageTestID");
            entity.Property(e => e.PackageId).HasColumnName("PackageID");
            entity.Property(e => e.TestId).HasColumnName("TestID");

            entity.HasOne(d => d.Package).WithMany(p => p.PackageTests)
                .HasForeignKey(d => d.PackageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PackageTe__Packa__52593CB8");

            entity.HasOne(d => d.Test).WithMany(p => p.PackageTests)
                .HasForeignKey(d => d.TestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PackageTe__TestI__534D60F1");
        });

        modelBuilder.Entity<PasswordReset>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Password__3214EC07D089F9CE");

            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.ExpireAt).HasColumnType("datetime");
            entity.Property(e => e.Token).HasMaxLength(255);
        });

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasKey(e => e.PatientId).HasName("PK__Patient__970EC346C6C586CD");

            entity.ToTable("Patient");

            entity.HasIndex(e => e.Email, "UQ__Patient__A9D105346F993B76").IsUnique();

            entity.Property(e => e.PatientId).HasColumnName("PatientID");
            entity.Property(e => e.Address).HasMaxLength(666);
            entity.Property(e => e.BloodGroup)
                .HasMaxLength(3)
                .IsUnicode(false);
            entity.Property(e => e.Dob)
                .HasColumnType("datetime")
                .HasColumnName("DOB");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.Gender).HasMaxLength(1);
            entity.Property(e => e.HealthInsurance)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.IsActive).HasColumnName("isActive");
            entity.Property(e => e.PasswordHash).HasMaxLength(1000);
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.ProfileImage).IsUnicode(false);
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.RoomId).HasName("PK__Room__328639192544B622");

            entity.ToTable("Room");

            entity.Property(e => e.RoomId).HasColumnName("RoomID");
            entity.Property(e => e.RoomName).HasMaxLength(100);
            entity.Property(e => e.Status).HasMaxLength(50);
        });

        modelBuilder.Entity<Schedule>(entity =>
        {
            entity.HasKey(e => e.ScheduleId).HasName("PK__Schedule__9C8A5B69B11484B7");

            entity.ToTable("Schedule");

            entity.Property(e => e.ScheduleId).HasColumnName("ScheduleID");
            entity.Property(e => e.DoctorId).HasColumnName("DoctorID");
            entity.Property(e => e.RoomId).HasColumnName("RoomID");
            entity.Property(e => e.SlotId).HasColumnName("SlotID");

            entity.HasOne(d => d.Doctor).WithMany(p => p.Schedules)
                .HasForeignKey(d => d.DoctorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Schedule__Doctor__45F365D3");

            entity.HasOne(d => d.Room).WithMany(p => p.Schedules)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Schedule__RoomID__47DBAE45");

            entity.HasOne(d => d.Slot).WithMany(p => p.Schedules)
                .HasForeignKey(d => d.SlotId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Schedule__SlotID__46E78A0C");
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.ServiceId).HasName("PK__Service__C51BB0EA6B9F812D");

            entity.ToTable("Service");

            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");
            entity.Property(e => e.ServicePrice).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.ServiceType).HasMaxLength(50);
        });

        modelBuilder.Entity<Slot>(entity =>
        {
            entity.HasKey(e => e.SlotId).HasName("PK__Slot__0A124A4FAD70CC2E");

            entity.ToTable("Slot");

            entity.Property(e => e.SlotId).HasColumnName("SlotID");
        });

        modelBuilder.Entity<Staff>(entity =>
        {
            entity.HasKey(e => e.StaffId).HasName("PK__Staff__96D4AAF704909B41");

            entity.HasIndex(e => e.Email, "UQ__Staff__A9D10534391B7C96").IsUnique();

            entity.Property(e => e.StaffId).HasColumnName("StaffID");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.Gender).HasMaxLength(1);
            entity.Property(e => e.IsActive).HasColumnName("isActive");
            entity.Property(e => e.PasswordHash).HasMaxLength(1000);
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.ProfileImage).IsUnicode(false);
            entity.Property(e => e.RoleName).HasMaxLength(30);
        });

        modelBuilder.Entity<Test>(entity =>
        {
            entity.HasKey(e => e.TestId).HasName("PK__Tests__8CC33100899456F4");

            entity.Property(e => e.TestId).HasColumnName("TestID");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");
        });

        modelBuilder.Entity<TestList>(entity =>
        {
            entity.HasKey(e => e.TestListId).HasName("PK__TestList__503ED0283EF0B98A");

            entity.ToTable("TestList");

            entity.Property(e => e.TestListId).HasColumnName("TestListID");
            entity.Property(e => e.AppointmentId).HasColumnName("AppointmentID");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.Result).HasMaxLength(255);
            entity.Property(e => e.TestId).HasColumnName("TestID");

            entity.HasOne(d => d.Appointment).WithMany(p => p.TestLists)
                .HasForeignKey(d => d.AppointmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TestList__Appoin__5FB337D6");

            entity.HasOne(d => d.Test).WithMany(p => p.TestLists)
                .HasForeignKey(d => d.TestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TestList__TestID__5EBF139D");
        });

        modelBuilder.Entity<Tracking>(entity =>
        {
            entity.HasKey(e => e.TrackingId).HasName("PK__Tracking__3C19EDD181B1F4B6");

            entity.ToTable("Tracking");

            entity.Property(e => e.TrackingId).HasColumnName("TrackingID");
            entity.Property(e => e.AppointmentId).HasColumnName("AppointmentID");
            entity.Property(e => e.RoomId).HasColumnName("RoomID");
            entity.Property(e => e.Time).HasColumnType("datetime");

            entity.HasOne(d => d.Appointment).WithMany(p => p.Trackings)
                .HasForeignKey(d => d.AppointmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Tracking__Appoin__66603565");

            entity.HasOne(d => d.Room).WithMany(p => p.Trackings)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Tracking__RoomID__6754599E");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
