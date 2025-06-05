using HospitalManagement.Data;
using HospitalManagement.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;

namespace HospitalManagement.Repositories
{
    public class BookingAppointmentRepository : IBookingAppointmentRepository
    {
        private readonly HospitalManagementContext _context;

        public BookingAppointmentRepository(HospitalManagementContext context)
        {
            _context = context;
        }

        public async Task<List<SelectListItem>> GetDoctorSelectListAsync()
        {
            return await _context.Doctors
         .Select(d => new SelectListItem
         {
             Value = d.DoctorId.ToString(),
             Text = d.FullName
         }).ToListAsync();
        }

        public async Task<List<SelectListItem>> GetSlotSelectListAsync()
        {
            return await _context.Slots
                .Select(s => new SelectListItem
                {
                    Value = s.SlotId.ToString(),
                    Text = s.StartTime.ToString(@"hh\:mm") + " - " + s.EndTime.ToString(@"hh\:mm")
                }).ToListAsync();
        }

        public async Task<Patient?> GetPatientByPatientIdAsync(int accountId)
        {
            return await _context.Patients.FirstOrDefaultAsync(p => p.PatientId == accountId);
        }

        public async Task AddAppointmentAsync(Appointment appointment)
        {
            await _context.Appointments.AddAsync(appointment);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

   
    }
}

