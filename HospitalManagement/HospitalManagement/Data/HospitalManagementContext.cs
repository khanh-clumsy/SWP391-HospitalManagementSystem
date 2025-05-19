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

    public virtual DbSet<Account> Accounts { get; set; }

    public virtual DbSet<Admin> Admins { get; set; }

    public virtual DbSet<Appointment> Appointments { get; set; }

    public virtual DbSet<AppointmentTest> AppointmentTests { get; set; }

    public virtual DbSet<AppointmentTracking> AppointmentTrackings { get; set; }

    public virtual DbSet<Doctor> Doctors { get; set; }

    public virtual DbSet<DoctorSpecialization> DoctorSpecializations { get; set; }

    public virtual DbSet<Feedback> Feedbacks { get; set; }

    public virtual DbSet<MedicalRecord> MedicalRecords { get; set; }

    public virtual DbSet<Medicine> Medicines { get; set; }

    public virtual DbSet<Patient> Patients { get; set; }

    public virtual DbSet<Prescription> Prescriptions { get; set; }

    public virtual DbSet<RequestConsultant> RequestConsultants { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Room> Rooms { get; set; }

    public virtual DbSet<Schedule> Schedules { get; set; }

    public virtual DbSet<Test> Tests { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=LAPTOP-GN00UMHT;Database=Hospital_Management;User ID=sa;Password=123;Encrypt=False;TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.AccountId).HasName("PK__Account__349DA586D8CB529A");

            entity.ToTable("Account");

            entity.HasIndex(e => e.Username, "UQ__Account__536C85E41ABCFB25").IsUnique();

            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.AdminId).HasColumnName("AdminID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.DoctorId).HasColumnName("DoctorID");
            entity.Property(e => e.Password).HasMaxLength(255);
            entity.Property(e => e.PatientId).HasColumnName("PatientID");
            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.Username).HasMaxLength(100);

            entity.HasOne(d => d.Admin).WithMany(p => p.Accounts)
                .HasForeignKey(d => d.AdminId)
                .HasConstraintName("FK__Account__AdminID__46E78A0C");

            entity.HasOne(d => d.Doctor).WithMany(p => p.Accounts)
                .HasForeignKey(d => d.DoctorId)
                .HasConstraintName("FK__Account__DoctorI__45F365D3");

            entity.HasOne(d => d.Patient).WithMany(p => p.Accounts)
                .HasForeignKey(d => d.PatientId)
                .HasConstraintName("FK__Account__Patient__44FF419A");

            entity.HasOne(d => d.Role).WithMany(p => p.Accounts)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Account__RoleID__440B1D61");
        });

        modelBuilder.Entity<Admin>(entity =>
        {
            entity.HasKey(e => e.AdminId).HasName("PK__Admin__719FE4E820E5B58D");

            entity.ToTable("Admin");

            entity.Property(e => e.AdminId).HasColumnName("AdminID");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
        });

        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasKey(e => e.AppointmentId).HasName("PK__Appointm__8ECDFCA223E02DC4");

            entity.ToTable("Appointment");

            entity.Property(e => e.AppointmentId).HasColumnName("AppointmentID");
            entity.Property(e => e.DoctorId).HasColumnName("DoctorID");
            entity.Property(e => e.PatientId).HasColumnName("PatientID");
            entity.Property(e => e.RoomId).HasColumnName("RoomID");
            entity.Property(e => e.Status).HasMaxLength(50);

            entity.HasOne(d => d.Doctor).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.DoctorId)
                .HasConstraintName("FK__Appointme__Docto__5070F446");

            entity.HasOne(d => d.Patient).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.PatientId)
                .HasConstraintName("FK__Appointme__Patie__4F7CD00D");

            entity.HasOne(d => d.Room).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.RoomId)
                .HasConstraintName("FK__Appointme__RoomI__5165187F");
        });

        modelBuilder.Entity<AppointmentTest>(entity =>
        {
            entity.HasKey(e => e.AppTestId).HasName("PK__Appointm__56690263E959F288");

            entity.Property(e => e.AppTestId).HasColumnName("AppTestID");
            entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.RecordId).HasColumnName("RecordID");
            entity.Property(e => e.Result).HasMaxLength(255);
            entity.Property(e => e.TestId).HasColumnName("TestID");

            entity.HasOne(d => d.Record).WithMany(p => p.AppointmentTests)
                .HasForeignKey(d => d.RecordId)
                .HasConstraintName("FK__Appointme__Recor__5DCAEF64");

            entity.HasOne(d => d.Test).WithMany(p => p.AppointmentTests)
                .HasForeignKey(d => d.TestId)
                .HasConstraintName("FK__Appointme__TestI__5EBF139D");
        });

        modelBuilder.Entity<AppointmentTracking>(entity =>
        {
            entity.HasKey(e => e.AppointmentTrId).HasName("PK__Appointm__2C968730D0B68D8E");

            entity.ToTable("AppointmentTracking");

            entity.Property(e => e.AppointmentTrId).HasColumnName("AppointmentTrID");
            entity.Property(e => e.AppointmentId).HasColumnName("AppointmentID");
            entity.Property(e => e.RoomNextId).HasColumnName("RoomNextID");
            entity.Property(e => e.Time).HasColumnType("datetime");

            entity.HasOne(d => d.Appointment).WithMany(p => p.AppointmentTrackings)
                .HasForeignKey(d => d.AppointmentId)
                .HasConstraintName("FK__Appointme__Appoi__5441852A");

            entity.HasOne(d => d.RoomNext).WithMany(p => p.AppointmentTrackings)
                .HasForeignKey(d => d.RoomNextId)
                .HasConstraintName("FK__Appointme__RoomN__5535A963");
        });

        modelBuilder.Entity<Doctor>(entity =>
        {
            entity.HasKey(e => e.DoctorId).HasName("PK__Doctor__2DC00EDF1A392C15");

            entity.ToTable("Doctor");

            entity.Property(e => e.DoctorId).HasColumnName("DoctorID");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.Gender).HasMaxLength(10);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.SpecializationId).HasColumnName("SpecializationID");

            entity.HasOne(d => d.Specialization).WithMany(p => p.Doctors)
                .HasForeignKey(d => d.SpecializationId)
                .HasConstraintName("FK__Doctor__Speciali__3D5E1FD2");
        });

        modelBuilder.Entity<DoctorSpecialization>(entity =>
        {
            entity.HasKey(e => e.SpecializationId).HasName("PK__DoctorSp__5809D84FF00367CA");

            entity.ToTable("DoctorSpecialization");

            entity.Property(e => e.SpecializationId).HasColumnName("SpecializationID");
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(e => e.FeedbackId).HasName("PK__Feedback__6A4BEDF6715093CB");

            entity.ToTable("Feedback");

            entity.Property(e => e.FeedbackId).HasColumnName("FeedbackID");
            entity.Property(e => e.AppointmentId).HasColumnName("AppointmentID");
            entity.Property(e => e.Comment).HasMaxLength(255);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Appointment).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.AppointmentId)
                .HasConstraintName("FK__Feedback__Appoin__693CA210");
        });

        modelBuilder.Entity<MedicalRecord>(entity =>
        {
            entity.HasKey(e => e.RecordId).HasName("PK__MedicalR__FBDF78C90BCC0489");

            entity.ToTable("MedicalRecord");

            entity.Property(e => e.RecordId).HasColumnName("RecordID");
            entity.Property(e => e.AppointmentId).HasColumnName("AppointmentID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Diagnosis).HasMaxLength(255);
            entity.Property(e => e.Note).HasMaxLength(255);
            entity.Property(e => e.Symptoms).HasMaxLength(255);
            entity.Property(e => e.TotalPrice).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.Appointment).WithMany(p => p.MedicalRecords)
                .HasForeignKey(d => d.AppointmentId)
                .HasConstraintName("FK__MedicalRe__Appoi__59063A47");
        });

        modelBuilder.Entity<Medicine>(entity =>
        {
            entity.HasKey(e => e.MedicineId).HasName("PK__Medicine__4F2128F020A80E77");

            entity.ToTable("Medicine");

            entity.Property(e => e.MedicineId).HasColumnName("MedicineID");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.MedicineType).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");
        });

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasKey(e => e.PatientId).HasName("PK__Patient__970EC346D4141EAB");

            entity.ToTable("Patient");

            entity.Property(e => e.PatientId).HasColumnName("PatientID");
            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.Dob).HasColumnName("DOB");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.Gender).HasMaxLength(10);
            entity.Property(e => e.Phone).HasMaxLength(20);
        });

        modelBuilder.Entity<Prescription>(entity =>
        {
            entity.HasKey(e => e.PrescriptionId).HasName("PK__Prescrip__40130812EF818D0A");

            entity.ToTable("Prescription");

            entity.Property(e => e.PrescriptionId).HasColumnName("PrescriptionID");
            entity.Property(e => e.MedicineId).HasColumnName("MedicineID");
            entity.Property(e => e.RecordId).HasColumnName("RecordID");
            entity.Property(e => e.TotalPrice).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.Medicine).WithMany(p => p.Prescriptions)
                .HasForeignKey(d => d.MedicineId)
                .HasConstraintName("FK__Prescript__Medic__6477ECF3");

            entity.HasOne(d => d.Record).WithMany(p => p.Prescriptions)
                .HasForeignKey(d => d.RecordId)
                .HasConstraintName("FK__Prescript__Recor__6383C8BA");
        });

        modelBuilder.Entity<RequestConsultant>(entity =>
        {
            entity.HasKey(e => e.RequestId).HasName("PK__RequestC__33A8519A00DAB6A7");

            entity.ToTable("RequestConsultant");

            entity.Property(e => e.RequestId).HasColumnName("RequestID");
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.DoctorId).HasColumnName("DoctorID");
            entity.Property(e => e.PatientId).HasColumnName("PatientID");
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.RequestedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ServicePrice).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.ServiceType).HasMaxLength(100);
            entity.Property(e => e.Status).HasMaxLength(50);

            entity.HasOne(d => d.Doctor).WithMany(p => p.RequestConsultants)
                .HasForeignKey(d => d.DoctorId)
                .HasConstraintName("FK__RequestCo__Docto__6E01572D");

            entity.HasOne(d => d.Patient).WithMany(p => p.RequestConsultants)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RequestCo__Patie__6D0D32F4");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Role__8AFACE3A970E1516");

            entity.ToTable("Role");

            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.RoomId).HasName("PK__Room__328639199A534D30");

            entity.ToTable("Room");

            entity.Property(e => e.RoomId).HasColumnName("RoomID");
            entity.Property(e => e.RoomNumber).HasMaxLength(10);
        });

        modelBuilder.Entity<Schedule>(entity =>
        {
            entity.HasKey(e => e.ScheduleId).HasName("PK__Schedule__9C8A5B697C040234");

            entity.ToTable("Schedule");

            entity.Property(e => e.ScheduleId).HasColumnName("ScheduleID");
            entity.Property(e => e.Day).HasMaxLength(20);
            entity.Property(e => e.DoctorId).HasColumnName("DoctorID");
            entity.Property(e => e.RoomId).HasColumnName("RoomID");

            entity.HasOne(d => d.Doctor).WithMany(p => p.Schedules)
                .HasForeignKey(d => d.DoctorId)
                .HasConstraintName("FK__Schedule__Doctor__4BAC3F29");

            entity.HasOne(d => d.Room).WithMany(p => p.Schedules)
                .HasForeignKey(d => d.RoomId)
                .HasConstraintName("FK__Schedule__RoomID__4CA06362");
        });

        modelBuilder.Entity<Test>(entity =>
        {
            entity.HasKey(e => e.TestId).HasName("PK__Tests__8CC3310075995918");

            entity.Property(e => e.TestId).HasColumnName("TestID");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
