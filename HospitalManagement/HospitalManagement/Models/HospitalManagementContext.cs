using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Models;

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

    public virtual DbSet<Appointment> Appointments { get; set; }

    public virtual DbSet<Consultant> Consultants { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<Feedback> Feedbacks { get; set; }

    public virtual DbSet<Invoice> Invoices { get; set; }

    public virtual DbSet<MedicalRecord> MedicalRecords { get; set; }

    public virtual DbSet<Medicine> Medicines { get; set; }

    public virtual DbSet<MedicineList> MedicineLists { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Room> Rooms { get; set; }

    public virtual DbSet<Schedule> Schedules { get; set; }

    public virtual DbSet<Service> Services { get; set; }

    public virtual DbSet<Slot> Slots { get; set; }

    public virtual DbSet<Test> Tests { get; set; }

    public virtual DbSet<TestList> TestLists { get; set; }

    public virtual DbSet<Tracking> Trackings { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=DESKTOP-D2P96LA;Database=Hospital_Management;User ID=sa;Password=123;Encrypt=False;TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.AccountId).HasName("PK__Account__349DA586BB610EB2");

            entity.ToTable("Account");

            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.DepartmentId).HasColumnName("DepartmentID");
            entity.Property(e => e.Dob).HasColumnName("DOB");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasColumnName("isActive");
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Username).HasMaxLength(50);

            entity.HasOne(d => d.Department).WithMany(p => p.Accounts)
                .HasForeignKey(d => d.DepartmentId)
                .HasConstraintName("FK__Account__Departm__3B75D760");

            entity.HasMany(d => d.Roles).WithMany(p => p.Accounts)
                .UsingEntity<Dictionary<string, object>>(
                    "AccountRole",
                    r => r.HasOne<Role>().WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__AccountRo__RoleI__3F466844"),
                    l => l.HasOne<Account>().WithMany()
                        .HasForeignKey("AccountId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__AccountRo__Accou__3E52440B"),
                    j =>
                    {
                        j.HasKey("AccountId", "RoleId").HasName("PK__AccountR__8C32096512A4737D");
                        j.ToTable("AccountRole");
                        j.IndexerProperty<int>("AccountId").HasColumnName("AccountID");
                        j.IndexerProperty<int>("RoleId").HasColumnName("RoleID");
                    });
        });

        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasKey(e => e.AppointmentId).HasName("PK__Appointm__8ECDFCA2B7809FC1");

            entity.ToTable("Appointment");

            entity.Property(e => e.AppointmentId).HasColumnName("AppointmentID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.DoctorAccountId).HasColumnName("DoctorAccountID");
            entity.Property(e => e.Status).HasMaxLength(50);

            entity.HasOne(d => d.Account).WithMany(p => p.AppointmentAccounts)
                .HasForeignKey(d => d.AccountId)
                .HasConstraintName("FK__Appointme__Accou__5165187F");

            entity.HasOne(d => d.DoctorAccount).WithMany(p => p.AppointmentDoctorAccounts)
                .HasForeignKey(d => d.DoctorAccountId)
                .HasConstraintName("FK__Appointme__Docto__534D60F1");

            entity.HasOne(d => d.ServiceTypeNavigation).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.ServiceType)
                .HasConstraintName("FK__Appointme__Servi__52593CB8");
        });

        modelBuilder.Entity<Consultant>(entity =>
        {
            entity.HasKey(e => e.ConsultantId).HasName("PK__Consulta__E5B83F39B317F659");

            entity.ToTable("Consultant");

            entity.Property(e => e.ConsultantId).HasColumnName("ConsultantID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.DoctorAccountId).HasColumnName("DoctorAccountID");
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Status).HasMaxLength(50);

            entity.HasOne(d => d.Account).WithMany(p => p.ConsultantAccounts)
                .HasForeignKey(d => d.AccountId)
                .HasConstraintName("FK__Consultan__Accou__4CA06362");

            entity.HasOne(d => d.DoctorAccount).WithMany(p => p.ConsultantDoctorAccounts)
                .HasForeignKey(d => d.DoctorAccountId)
                .HasConstraintName("FK__Consultan__Docto__4E88ABD4");

            entity.HasOne(d => d.ServiceTypeNavigation).WithMany(p => p.Consultants)
                .HasForeignKey(d => d.ServiceType)
                .HasConstraintName("FK__Consultan__Servi__4D94879B");
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.DepartmentId).HasName("PK__Departme__B2079BCD31D51DEF");

            entity.ToTable("Department");

            entity.Property(e => e.DepartmentId).HasColumnName("DepartmentID");
            entity.Property(e => e.DepartmentName).HasMaxLength(100);
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(e => e.FeedbackId).HasName("PK__Feedback__6A4BEDF60EFA2153");

            entity.ToTable("Feedback");

            entity.Property(e => e.FeedbackId).HasColumnName("FeedbackID");
            entity.Property(e => e.AppointmentId).HasColumnName("AppointmentID");
            entity.Property(e => e.Comment).HasMaxLength(255);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Appointment).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.AppointmentId)
                .HasConstraintName("FK__Feedback__Appoin__59FA5E80");
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.InvoiceId).HasName("PK__Invoice__D796AAD5B156F1F2");

            entity.ToTable("Invoice");

            entity.Property(e => e.InvoiceId).HasColumnName("InvoiceID");
            entity.Property(e => e.AppointmentId).HasColumnName("AppointmentID");
            entity.Property(e => e.ConsultantId).HasColumnName("ConsultantID");
            entity.Property(e => e.MedicinePrice).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Method).HasMaxLength(50);
            entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.TransactionCode).HasMaxLength(100);

            entity.HasOne(d => d.Appointment).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.AppointmentId)
                .HasConstraintName("FK__Invoice__Appoint__5629CD9C");

            entity.HasOne(d => d.Consultant).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.ConsultantId)
                .HasConstraintName("FK__Invoice__Consult__571DF1D5");
        });

        modelBuilder.Entity<MedicalRecord>(entity =>
        {
            entity.HasKey(e => e.MedicalRecordId).HasName("PK__MedicalR__4411BBC2F3DCFDBE");

            entity.ToTable("MedicalRecord");

            entity.Property(e => e.MedicalRecordId).HasColumnName("MedicalRecordID");
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
                .HasConstraintName("FK__MedicalRe__Appoi__5DCAEF64");
        });

        modelBuilder.Entity<Medicine>(entity =>
        {
            entity.HasKey(e => e.MedicineId).HasName("PK__Medicine__4F2128F0C1CEBFA7");

            entity.ToTable("Medicine");

            entity.Property(e => e.MedicineId).HasColumnName("MedicineID");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.MedicineType).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Unit).HasMaxLength(50);
        });

        modelBuilder.Entity<MedicineList>(entity =>
        {
            entity.HasKey(e => new { e.MedicalRecordId, e.MedicineId }).HasName("PK__Medicine__50E3A94DDC1F37B7");

            entity.ToTable("MedicineList");

            entity.Property(e => e.MedicalRecordId).HasColumnName("MedicalRecordID");
            entity.Property(e => e.MedicineId).HasColumnName("MedicineID");

            entity.HasOne(d => d.MedicalRecord).WithMany(p => p.MedicineLists)
                .HasForeignKey(d => d.MedicalRecordId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__MedicineL__Medic__693CA210");

            entity.HasOne(d => d.Medicine).WithMany(p => p.MedicineLists)
                .HasForeignKey(d => d.MedicineId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__MedicineL__Medic__6A30C649");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Role__8AFACE3A91FD3B90");

            entity.ToTable("Role");

            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.RoomId).HasName("PK__Room__32863919051154CE");

            entity.ToTable("Room");

            entity.Property(e => e.RoomId).HasColumnName("RoomID");
            entity.Property(e => e.RoomName).HasMaxLength(100);
        });

        modelBuilder.Entity<Schedule>(entity =>
        {
            entity.HasKey(e => e.ScheduleId).HasName("PK__Schedule__9C8A5B69EDFC9AAB");

            entity.ToTable("Schedule");

            entity.Property(e => e.ScheduleId).HasColumnName("ScheduleID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.RoomId).HasColumnName("RoomID");
            entity.Property(e => e.SlotId).HasColumnName("SlotID");

            entity.HasOne(d => d.Account).WithMany(p => p.Schedules)
                .HasForeignKey(d => d.AccountId)
                .HasConstraintName("FK__Schedule__Accoun__45F365D3");

            entity.HasOne(d => d.Room).WithMany(p => p.Schedules)
                .HasForeignKey(d => d.RoomId)
                .HasConstraintName("FK__Schedule__RoomID__46E78A0C");

            entity.HasOne(d => d.Slot).WithMany(p => p.Schedules)
                .HasForeignKey(d => d.SlotId)
                .HasConstraintName("FK__Schedule__SlotID__47DBAE45");
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.ServiceType).HasName("PK__Service__0E5C2012FC770DA3");

            entity.ToTable("Service");

            entity.Property(e => e.ServicePrice).HasColumnType("decimal(10, 2)");
        });

        modelBuilder.Entity<Slot>(entity =>
        {
            entity.HasKey(e => e.SlotId).HasName("PK__Slot__0A124A4F83D4E14F");

            entity.ToTable("Slot");

            entity.Property(e => e.SlotId).HasColumnName("SlotID");
        });

        modelBuilder.Entity<Test>(entity =>
        {
            entity.HasKey(e => e.TestId).HasName("PK__Tests__8CC331005C50BE6A");

            entity.Property(e => e.TestId).HasColumnName("TestID");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");
        });

        modelBuilder.Entity<TestList>(entity =>
        {
            entity.HasKey(e => new { e.MedicalRecordId, e.TestId }).HasName("PK__TestList__5CDD88D2AF844C57");

            entity.ToTable("TestList");

            entity.Property(e => e.MedicalRecordId).HasColumnName("MedicalRecordID");
            entity.Property(e => e.TestId).HasColumnName("TestID");
            entity.Property(e => e.Result).HasMaxLength(255);

            entity.HasOne(d => d.MedicalRecord).WithMany(p => p.TestLists)
                .HasForeignKey(d => d.MedicalRecordId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TestList__Medica__6383C8BA");

            entity.HasOne(d => d.Test).WithMany(p => p.TestLists)
                .HasForeignKey(d => d.TestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TestList__TestID__6477ECF3");
        });

        modelBuilder.Entity<Tracking>(entity =>
        {
            entity.HasKey(e => new { e.AppointmentId, e.RoomId }).HasName("PK__Tracking__0DE59F33330D521C");

            entity.ToTable("Tracking");

            entity.Property(e => e.AppointmentId).HasColumnName("AppointmentID");
            entity.Property(e => e.RoomId).HasColumnName("RoomID");
            entity.Property(e => e.Time).HasColumnType("datetime");

            entity.HasOne(d => d.Appointment).WithMany(p => p.Trackings)
                .HasForeignKey(d => d.AppointmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Tracking__Appoin__6D0D32F4");

            entity.HasOne(d => d.Room).WithMany(p => p.Trackings)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Tracking__RoomID__6E01572D");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
