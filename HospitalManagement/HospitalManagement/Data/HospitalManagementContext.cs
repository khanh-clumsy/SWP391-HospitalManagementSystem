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

    public virtual DbSet<ScheduleChangeRequest> ScheduleChangeRequests { get; set; }

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
            entity.HasKey(e => e.AppointmentId).HasName("PK__Appointm__8ECDFCA205E07511");

            entity.ToTable("Appointment");

            entity.HasIndex(e => e.Date, "IX_Appointment_Date");

            entity.HasIndex(e => e.DoctorId, "IX_Appointment_DoctorID");

            entity.HasIndex(e => e.PackageId, "IX_Appointment_PackageID");

            entity.HasIndex(e => e.PatientId, "IX_Appointment_PatientID");

            entity.HasIndex(e => e.ServiceId, "IX_Appointment_ServiceID");

            entity.HasIndex(e => e.SlotId, "IX_Appointment_SlotID");

            entity.HasIndex(e => e.StaffId, "IX_Appointment_StaffID");

            entity.Property(e => e.AppointmentId).HasColumnName("AppointmentID");
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
                .HasConstraintName("FK__Appointme__Docto__5629CD9C");

            entity.HasOne(d => d.Package).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.PackageId)
                .HasConstraintName("FK__Appointme__Packa__59063A47");

            entity.HasOne(d => d.Patient).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Appointme__Patie__571DF1D5");

            entity.HasOne(d => d.Service).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.ServiceId)
                .HasConstraintName("FK__Appointme__Servi__5812160E");

            entity.HasOne(d => d.Slot).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.SlotId)
                .HasConstraintName("FK__Appointme__SlotI__5AEE82B9");

            entity.HasOne(d => d.Staff).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.StaffId)
                .HasConstraintName("FK__Appointme__Staff__59FA5E80");
        });

        modelBuilder.Entity<Doctor>(entity =>
        {
            entity.HasKey(e => e.DoctorId).HasName("PK__Doctor__2DC00EDFC306780E");

            entity.ToTable("Doctor");

            entity.HasIndex(e => e.FullName, "IX_Doctor_FullName");

            entity.HasIndex(e => e.Email, "UQ__Doctor__A9D105347E3B1461").IsUnique();

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
            entity.HasKey(e => e.FeedbackId).HasName("PK__Feedback__6A4BEDF69061150F");

            entity.ToTable("Feedback");

            entity.HasIndex(e => e.PackageId, "IX_Feedback_PackageID");

            entity.HasIndex(e => e.PatientId, "IX_Feedback_PatientID");

            entity.HasIndex(e => e.ServiceId, "IX_Feedback_ServiceID");

            entity.Property(e => e.FeedbackId).HasColumnName("FeedbackID");
            entity.Property(e => e.Comment).HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.PackageId).HasColumnName("PackageID");
            entity.Property(e => e.PatientId).HasColumnName("PatientID");
            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");

            entity.HasOne(d => d.Package).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.PackageId)
                .HasConstraintName("FK__Feedback__Packag__6477ECF3");

            entity.HasOne(d => d.Patient).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Feedback__Patien__628FA481");

            entity.HasOne(d => d.Service).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.ServiceId)
                .HasConstraintName("FK__Feedback__Servic__6383C8BA");
        });

        modelBuilder.Entity<News>(entity =>
        {
            entity.HasKey(e => e.NewsId).HasName("PK__News__954EBDD3B25DC3BE");

            entity.HasIndex(e => e.DoctorId, "IX_News_DoctorID");

            entity.HasIndex(e => e.StaffId, "IX_News_StaffID");

            entity.Property(e => e.NewsId).HasColumnName("NewsID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(4000);
            entity.Property(e => e.DoctorId).HasColumnName("DoctorID");
            entity.Property(e => e.StaffId).HasColumnName("StaffID");
            entity.Property(e => e.Title).HasMaxLength(500);

            entity.HasOne(d => d.Doctor).WithMany(p => p.News)
                .HasForeignKey(d => d.DoctorId)
                .HasConstraintName("FK__News__DoctorID__6FE99F9F");

            entity.HasOne(d => d.Staff).WithMany(p => p.News)
                .HasForeignKey(d => d.StaffId)
                .HasConstraintName("FK__News__StaffID__6EF57B66");
        });

        modelBuilder.Entity<Package>(entity =>
        {
            entity.HasKey(e => e.PackageId).HasName("PK__Package__322035ECECFD6C27");

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
            entity.HasKey(e => e.PackageCategoryId).HasName("PK__PackageC__BAB8ED7E99A345C5");

            entity.ToTable("PackageCategory");

            entity.Property(e => e.PackageCategoryId).HasColumnName("PackageCategoryID");
            entity.Property(e => e.CategoryName).HasMaxLength(100);
        });

        modelBuilder.Entity<PackageTest>(entity =>
        {
            entity.HasKey(e => e.PackageTestId).HasName("PK__PackageT__DB90EDCE55FD8ED0");

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
            entity.HasKey(e => e.Id).HasName("PK__Password__3214EC07A2474F9C");

            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.ExpireAt).HasColumnType("datetime");
            entity.Property(e => e.Token).HasMaxLength(255);
        });

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasKey(e => e.PatientId).HasName("PK__Patient__970EC3468488C11E");

            entity.ToTable("Patient");

            entity.HasIndex(e => e.FullName, "IX_Patient_FullName");

            entity.HasIndex(e => e.Email, "UQ__Patient__A9D1053484B885EA").IsUnique();

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
            entity.HasKey(e => e.RoomId).HasName("PK__Room__32863919A6FE34A5");

            entity.ToTable("Room");

            entity.Property(e => e.RoomId).HasColumnName("RoomID");
            entity.Property(e => e.RoomName).HasMaxLength(100);
            entity.Property(e => e.RoomType).HasMaxLength(100);
            entity.Property(e => e.Status).HasMaxLength(50);
        });

        modelBuilder.Entity<Schedule>(entity =>
        {
            entity.HasKey(e => e.ScheduleId).HasName("PK__Schedule__9C8A5B69754DB967");

            entity.ToTable("Schedule");

            entity.HasIndex(e => e.Day, "IX_Schedule_Day");

            entity.HasIndex(e => e.DoctorId, "IX_Schedule_DoctorID");

            entity.HasIndex(e => e.RoomId, "IX_Schedule_RoomID");

            entity.HasIndex(e => e.SlotId, "IX_Schedule_SlotID");

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

        modelBuilder.Entity<ScheduleChangeRequest>(entity =>
        {
            entity.HasKey(e => e.RequestId).HasName("PK__Schedule__33A8519A628B4F44");

            entity.ToTable("ScheduleChangeRequest");

            entity.Property(e => e.RequestId).HasColumnName("RequestID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.DoctorId).HasColumnName("DoctorID");
            entity.Property(e => e.FromScheduleId).HasColumnName("FromScheduleID");
            entity.Property(e => e.Reason).HasMaxLength(1000);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Pending");
            entity.Property(e => e.ToRoomId).HasColumnName("ToRoomID");
            entity.Property(e => e.ToSlotId).HasColumnName("ToSlotID");

            entity.HasOne(d => d.Doctor).WithMany(p => p.ScheduleChangeRequests)
                .HasForeignKey(d => d.DoctorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ScheduleC__Docto__74AE54BC");

            entity.HasOne(d => d.FromSchedule).WithMany(p => p.ScheduleChangeRequests)
                .HasForeignKey(d => d.FromScheduleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ScheduleC__FromS__75A278F5");

            entity.HasOne(d => d.ToRoom).WithMany(p => p.ScheduleChangeRequests)
                .HasForeignKey(d => d.ToRoomId)
                .HasConstraintName("FK__ScheduleC__ToRoo__778AC167");

            entity.HasOne(d => d.ToSlot).WithMany(p => p.ScheduleChangeRequests)
                .HasForeignKey(d => d.ToSlotId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ScheduleC__ToSlo__76969D2E");
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.ServiceId).HasName("PK__Service__C51BB0EA1B7FCA69");

            entity.ToTable("Service");

            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");
            entity.Property(e => e.ServicePrice).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.ServiceType).HasMaxLength(50);
        });

        modelBuilder.Entity<Slot>(entity =>
        {
            entity.HasKey(e => e.SlotId).HasName("PK__Slot__0A124A4FFADFF86C");

            entity.ToTable("Slot");

            entity.Property(e => e.SlotId).HasColumnName("SlotID");
        });

        modelBuilder.Entity<Staff>(entity =>
        {
            entity.HasKey(e => e.StaffId).HasName("PK__Staff__96D4AAF7CD775EDB");

            entity.HasIndex(e => e.FullName, "IX_Staff_FullName");

            entity.HasIndex(e => e.Email, "UQ__Staff__A9D10534E2F9B160").IsUnique();

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
            entity.HasKey(e => e.TestId).HasName("PK__Tests__8CC331004905A880");

            entity.Property(e => e.TestId).HasColumnName("TestID");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.RoomType).HasMaxLength(100);
        });

        modelBuilder.Entity<TestList>(entity =>
        {
            entity.HasKey(e => e.TestListId).HasName("PK__TestList__503ED028DD5DB361");

            entity.ToTable("TestList");

            entity.HasIndex(e => e.AppointmentId, "IX_TestList_AppointmentID");

            entity.HasIndex(e => e.TestId, "IX_TestList_TestID");

            entity.Property(e => e.TestListId).HasColumnName("TestListID");
            entity.Property(e => e.AppointmentId).HasColumnName("AppointmentID");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.Result).HasMaxLength(255);
            entity.Property(e => e.TestId).HasColumnName("TestID");
            entity.Property(e => e.TestStatus)
                .HasMaxLength(50)
                .HasDefaultValue("Ongoing");

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
            entity.HasKey(e => e.TrackingId).HasName("PK__Tracking__3C19EDD1075E6B51");

            entity.ToTable("Tracking");

            entity.HasIndex(e => e.AppointmentId, "IX_Tracking_AppointmentID");

            entity.HasIndex(e => e.RoomId, "IX_Tracking_RoomID");

            entity.Property(e => e.TrackingId).HasColumnName("TrackingID");
            entity.Property(e => e.AppointmentId).HasColumnName("AppointmentID");
            entity.Property(e => e.RoomId).HasColumnName("RoomID");
            entity.Property(e => e.TestListId).HasColumnName("TestListID");
            entity.Property(e => e.Time).HasColumnType("datetime");

            entity.HasOne(d => d.Appointment).WithMany(p => p.Trackings)
                .HasForeignKey(d => d.AppointmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Tracking__Appoin__6754599E");

            entity.HasOne(d => d.Room).WithMany(p => p.Trackings)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Tracking__RoomID__693CA210");

            entity.HasOne(d => d.TestList).WithMany(p => p.Trackings)
                .HasForeignKey(d => d.TestListId)
                .HasConstraintName("FK__Tracking__TestLi__68487DD7");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
