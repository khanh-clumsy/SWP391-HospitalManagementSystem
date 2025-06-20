﻿using HospitalManagement.Data;
using HospitalManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Repositories
{
    public class AppointmentRepository : IAppointmentRepository
    {
        private readonly HospitalManagementContext _context;

        public AppointmentRepository(HospitalManagementContext context)
        {
            _context = context;
        }

        public async Task<List<Appointment>> Filter(string RoleKey, int UserID, string? Name, string? slotId, string? Date, string? Status)
        {
            var query = _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Slot)
                .Include(a => a.Package)
                .Include(a => a.Service)
                .Where(a =>
                    (RoleKey == "PatientID" && a.PatientId == UserID) ||
                    (RoleKey == "StaffID" && a.StaffId == UserID) ||
                    (RoleKey == "DoctorID" && a.DoctorId == UserID))
                .AsQueryable();

            if (!string.IsNullOrEmpty(Name))
            {
                Name = string.Join(" ", Name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));

                query = RoleKey switch
                {
                    "PatientID" => query.Where(a => a.Doctor.FullName.Contains(Name)),
                    _ => query.Where(a => a.Patient.FullName.Contains(Name))
                };
            }

            if (!string.IsNullOrEmpty(slotId) && int.TryParse(slotId, out int parsedSlotId))
            {
                query = query.Where(a => a.SlotId == parsedSlotId);
            }

            if (!string.IsNullOrEmpty(Date) && DateTime.TryParse(Date, out var parsedDate))
            {
                var convertedDate = DateOnly.FromDateTime(parsedDate);
                query = query.Where(a => a.Date == convertedDate);
            }

            if (!string.IsNullOrEmpty(Status))
            {
                query = query.Where(a => a.Status == Status);
            }
            return await query.ToListAsync();
        }
        public async Task<List<Appointment>> FilterForAdmin(string? Name, string? slotId, string? Date, string? Status)
        {
            var query = _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Slot)
                .AsQueryable();

            if (!string.IsNullOrEmpty(Name))
            {
                Name = Name.Trim();
                query = query.Where(a => a.Patient.FullName.Contains(Name));
            }

            if (!string.IsNullOrEmpty(slotId) && int.TryParse(slotId, out int parsedSlotId))
            {
                query = query.Where(a => a.SlotId == parsedSlotId);
            }

            if (!string.IsNullOrEmpty(Date) && DateTime.TryParse(Date, out var parsedDate))
            {
                var convertedDate = DateOnly.FromDateTime(parsedDate);
                query = query.Where(a => a.Date == convertedDate);
            }

            if (!string.IsNullOrEmpty(Status))
            {
                query = query.Where(a => a.Status == Status);
            }

            return await query.ToListAsync();
        }


        public IQueryable<Appointment> GetAppointmentByDoctorID(int DoctorID)
        {
            return _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Staff)
                .Include(a => a.Slot)
                .Include(a => a.Service)
                .Where(a => a.DoctorId == DoctorID)
                .OrderByDescending(a => a.AppointmentId);
        }

        public IQueryable<Appointment> GetAppointmentByPatientID(int PatientID)
        {
            return _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Staff)
                .Include(a => a.Slot)
                .Include(a => a.Service)
                .Where(a => a.PatientId == PatientID)
                .OrderByDescending(a => a.AppointmentId);
        }

        public IQueryable<Appointment> GetAppointmentBySalesID(int SalesID)
        {
            return _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Staff)
                .Include(a => a.Slot)
                .Include(a => a.Service)
                .Where(a => a.StaffId == SalesID)
                .OrderByDescending(a => a.AppointmentId);
        }

        public async Task<Appointment?> GetByIdAsync(int id)
        {
            return await _context.Appointments.FindAsync(id);
        }

        public async Task DeleteAsync(Appointment appointment)
        {
            _context.Appointments.Remove(appointment);
            await _context.SaveChangesAsync();
        }

    }
}
