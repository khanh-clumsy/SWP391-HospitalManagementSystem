using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using HospitalManagement.Models;
using HospitalManagement.Services;
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

    public virtual DbSet<InvoiceDetail> InvoiceDetails { get; set; }

    public virtual DbSet<News> News { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

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

    public virtual DbSet<TestRecord> TestRecords { get; set; }

    public virtual DbSet<Tracking> Trackings { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=localhost;Database=Hospital_Management;User ID=sa;Password=123;Encrypt=False;TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Áp dụng soft delete cho tất cả entity implement ISoftDelete
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(ISoftDelete.IsDeleted));
                var filter = Expression.Lambda(Expression.Equal(property, Expression.Constant(false)), parameter);
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
            }
        }
        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasKey(e => e.AppointmentId).HasName("PK__Appointm__8ECDFCA2F3323827");

            entity.ToTable("Appointment");

            entity.HasIndex(e => e.CreatedByStaffId, "IX_Appointment_CreatedByStaffID");

            entity.HasIndex(e => e.Date, "IX_Appointment_Date");

            entity.HasIndex(e => e.DoctorId, "IX_Appointment_DoctorID");

            entity.HasIndex(e => e.PackageId, "IX_Appointment_PackageID");

            entity.HasIndex(e => e.PatientId, "IX_Appointment_PatientID");

            entity.HasIndex(e => e.ServiceId, "IX_Appointment_ServiceID");

            entity.HasIndex(e => e.SlotId, "IX_Appointment_SlotID");

            entity.Property(e => e.AppointmentId).HasColumnName("AppointmentID");
            entity.Property(e => e.CreatedByStaffId).HasColumnName("CreatedByStaffID");
            entity.Property(e => e.Diagnosis).HasMaxLength(100);
            entity.Property(e => e.DoctorId).HasColumnName("DoctorID");
            entity.Property(e => e.Note).HasMaxLength(255);
            entity.Property(e => e.PackageId).HasColumnName("PackageID");
            entity.Property(e => e.PatientId).HasColumnName("PatientID");
            entity.Property(e => e.PaymentStatus).HasMaxLength(20);
            entity.Property(e => e.RecordCreatedAt).HasColumnType("datetime");
            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");
            entity.Property(e => e.SlotId).HasColumnName("SlotID");
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.Symptoms).HasMaxLength(100);
            entity.Property(e => e.TotalPrice).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.CreatedByStaff).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.CreatedByStaffId)
                .HasConstraintName("FK__Appointme__Creat__59FA5E80");

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
        });

        modelBuilder.Entity<Doctor>(entity =>
        {
            entity.HasKey(e => e.DoctorId).HasName("PK__Doctor__2DC00EDFB6BC65C6");

            entity.ToTable("Doctor");

            entity.HasIndex(e => e.FullName, "IX_Doctor_FullName");

            entity.HasIndex(e => e.Email, "UQ__Doctor__A9D10534E37BF852").IsUnique();

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
            entity.HasKey(e => e.FeedbackId).HasName("PK__Feedback__6A4BEDF6B0B08AEC");

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
                .HasConstraintName("FK__Feedback__Packag__656C112C");

            entity.HasOne(d => d.Patient).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Feedback__Patien__6383C8BA");

            entity.HasOne(d => d.Service).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.ServiceId)
                .HasConstraintName("FK__Feedback__Servic__6477ECF3");
        });

        modelBuilder.Entity<InvoiceDetail>(entity =>
        {
            entity.HasKey(e => e.InvoiceDetailId).HasName("PK__InvoiceD__1F1578F11959B5E3");

            entity.ToTable("InvoiceDetail");

            entity.Property(e => e.InvoiceDetailId).HasColumnName("InvoiceDetailID");
            entity.Property(e => e.AppointmentId).HasColumnName("AppointmentID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(NULL)")
                .HasColumnType("datetime");
            entity.Property(e => e.ItemId).HasColumnName("ItemID");
            entity.Property(e => e.ItemName).HasMaxLength(255);
            entity.Property(e => e.ItemType).HasMaxLength(50);
            entity.Property(e => e.PaymentMethod).HasMaxLength(20);
            entity.Property(e => e.PaymentStatus).HasMaxLength(20);
            entity.Property(e => e.PaymentTime).HasColumnType("datetime");
            entity.Property(e => e.ResponseCode).HasMaxLength(10);
            entity.Property(e => e.TransactionCode).HasMaxLength(100);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.Appointment).WithMany(p => p.InvoiceDetails)
                .HasForeignKey(d => d.AppointmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__InvoiceDe__Appoi__71D1E811");
        });

        modelBuilder.Entity<News>(entity =>
        {
            entity.HasKey(e => e.NewsId).HasName("PK__News__954EBDD3A9D4010E");

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
                .HasConstraintName("FK__News__DoctorID__787EE5A0");

            entity.HasOne(d => d.Staff).WithMany(p => p.News)
                .HasForeignKey(d => d.StaffId)
                .HasConstraintName("FK__News__StaffID__778AC167");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__20CF2E325CBBF97F");

            entity.ToTable("Notification");

            entity.Property(e => e.NotificationId).HasColumnName("NotificationID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsRead).HasDefaultValue(false);
            entity.Property(e => e.Link).HasMaxLength(255);
            entity.Property(e => e.Message).HasMaxLength(255);
            entity.Property(e => e.NotificationType).HasMaxLength(50);
            entity.Property(e => e.RefId).HasColumnName("RefID");
            entity.Property(e => e.Role).HasMaxLength(20);
            entity.Property(e => e.Title).HasMaxLength(100);
        });

        modelBuilder.Entity<Package>(entity =>
        {
            entity.HasKey(e => e.PackageId).HasName("PK__Package__322035EC201961A3");

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
            entity.HasKey(e => e.PackageCategoryId).HasName("PK__PackageC__BAB8ED7EA3AEC0EA");

            entity.ToTable("PackageCategory");

            entity.Property(e => e.PackageCategoryId).HasColumnName("PackageCategoryID");
            entity.Property(e => e.CategoryName).HasMaxLength(100);
        });

        modelBuilder.Entity<PackageTest>(entity =>
        {
            entity.HasKey(e => e.PackageTestId).HasName("PK__PackageT__DB90EDCEE03C1075");

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
            entity.HasKey(e => e.Id).HasName("PK__Password__3214EC0783267F32");

            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.ExpireAt).HasColumnType("datetime");
            entity.Property(e => e.Token).HasMaxLength(255);
        });

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasKey(e => e.PatientId).HasName("PK__Patient__970EC346E3FE401A");

            entity.ToTable("Patient");

            entity.HasIndex(e => e.FullName, "IX_Patient_FullName");

            entity.HasIndex(e => e.Email, "UQ__Patient__A9D10534C5F0C521").IsUnique();

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
            entity.HasKey(e => e.RoomId).HasName("PK__Room__32863919DD54F968");

            entity.ToTable("Room");

            entity.Property(e => e.RoomId).HasColumnName("RoomID");
            entity.Property(e => e.RoomName).HasMaxLength(100);
            entity.Property(e => e.RoomType).HasMaxLength(100);
            entity.Property(e => e.Status).HasMaxLength(50);
        });

        modelBuilder.Entity<Schedule>(entity =>
        {
            entity.HasKey(e => e.ScheduleId).HasName("PK__Schedule__9C8A5B69E31475B8");

            entity.ToTable("Schedule");

            entity.HasIndex(e => e.Day, "IX_Schedule_Day");

            entity.HasIndex(e => e.DoctorId, "IX_Schedule_DoctorID");

            entity.HasIndex(e => e.RoomId, "IX_Schedule_RoomID");

            entity.HasIndex(e => e.SlotId, "IX_Schedule_SlotID");

            entity.Property(e => e.ScheduleId).HasColumnName("ScheduleID");
            entity.Property(e => e.DoctorId).HasColumnName("DoctorID");
            entity.Property(e => e.RoomId).HasColumnName("RoomID");
            entity.Property(e => e.SlotId).HasColumnName("SlotID");
            entity.Property(e => e.Status).HasMaxLength(20);

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
            entity.HasKey(e => e.RequestId).HasName("PK__Schedule__33A8519A331442A3");

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
                .HasConstraintName("FK__ScheduleC__Docto__7D439ABD");

            entity.HasOne(d => d.FromSchedule).WithMany(p => p.ScheduleChangeRequests)
                .HasForeignKey(d => d.FromScheduleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ScheduleC__FromS__7E37BEF6");

            entity.HasOne(d => d.ToRoom).WithMany(p => p.ScheduleChangeRequests)
                .HasForeignKey(d => d.ToRoomId)
                .HasConstraintName("FK__ScheduleC__ToRoo__00200768");

            entity.HasOne(d => d.ToSlot).WithMany(p => p.ScheduleChangeRequests)
                .HasForeignKey(d => d.ToSlotId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ScheduleC__ToSlo__7F2BE32F");
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.ServiceId).HasName("PK__Service__C51BB0EA6C42CECE");

            entity.ToTable("Service");

            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");
            entity.Property(e => e.ServicePrice).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.ServiceType).HasMaxLength(50);
        });

        modelBuilder.Entity<Slot>(entity =>
        {
            entity.HasKey(e => e.SlotId).HasName("PK__Slot__0A124A4FD7CFA1B1");

            entity.ToTable("Slot");

            entity.Property(e => e.SlotId).HasColumnName("SlotID");
        });

        modelBuilder.Entity<Staff>(entity =>
        {
            entity.HasKey(e => e.StaffId).HasName("PK__Staff__96D4AAF7366AB645");

            entity.HasIndex(e => e.FullName, "IX_Staff_FullName");

            entity.HasIndex(e => e.Email, "UQ__Staff__A9D10534D948F28C").IsUnique();

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
            entity.HasKey(e => e.TestId).HasName("PK__Tests__8CC331001B188BA0");

            entity.Property(e => e.TestId).HasColumnName("TestID");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.RoomType).HasMaxLength(100);
        });

        modelBuilder.Entity<TestRecord>(entity =>
        {
            entity.HasKey(e => e.TestRecordId).HasName("PK__TestReco__1FA0FE96A7A462FE");

            entity.ToTable("TestRecord");

            entity.HasIndex(e => e.AppointmentId, "IX_TestRecord_AppointmentID");

            entity.HasIndex(e => e.TestId, "IX_TestRecord_TestID");

            entity.Property(e => e.TestRecordId).HasColumnName("TestRecordID");
            entity.Property(e => e.AppointmentId).HasColumnName("AppointmentID");
            entity.Property(e => e.CompletedAt).HasColumnType("datetime");
            entity.Property(e => e.DoctorId).HasColumnName("DoctorID");
            entity.Property(e => e.Result).HasMaxLength(255);
            entity.Property(e => e.TestId).HasColumnName("TestID");
            entity.Property(e => e.TestNote).HasMaxLength(255);
            entity.Property(e => e.TestStatus)
                .HasMaxLength(50)
                .HasDefaultValue("Ongoing");

            entity.HasOne(d => d.Appointment).WithMany(p => p.TestRecords)
                .HasForeignKey(d => d.AppointmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TestRecor__Appoi__5FB337D6");

            entity.HasOne(d => d.Doctor).WithMany(p => p.TestRecords)
                .HasForeignKey(d => d.DoctorId)
                .HasConstraintName("FK__TestRecor__Docto__60A75C0F");

            entity.HasOne(d => d.Test).WithMany(p => p.TestRecords)
                .HasForeignKey(d => d.TestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TestRecor__TestI__5EBF139D");
        });

        modelBuilder.Entity<Tracking>(entity =>
        {
            entity.HasKey(e => e.TrackingId).HasName("PK__Tracking__3C19EDD1593E0E7F");

            entity.ToTable("Tracking");

            entity.HasIndex(e => e.AppointmentId, "IX_Tracking_AppointmentID");

            entity.HasIndex(e => e.RoomId, "IX_Tracking_RoomID");

            entity.Property(e => e.TrackingId).HasColumnName("TrackingID");
            entity.Property(e => e.AppointmentId).HasColumnName("AppointmentID");
            entity.Property(e => e.RoomId).HasColumnName("RoomID");
            entity.Property(e => e.TestRecordId).HasColumnName("TestRecordID");
            entity.Property(e => e.Time).HasColumnType("datetime");

            entity.HasOne(d => d.Appointment).WithMany(p => p.Trackings)
                .HasForeignKey(d => d.AppointmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Tracking__Appoin__68487DD7");

            entity.HasOne(d => d.Room).WithMany(p => p.Trackings)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Tracking__RoomID__6A30C649");

            entity.HasOne(d => d.TestRecord).WithMany(p => p.Trackings)
                .HasForeignKey(d => d.TestRecordId)
                .HasConstraintName("FK__Tracking__TestRe__693CA210");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
