using HospitalManagement.Data;
using HospitalManagement.Helpers;
using HospitalManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Services
{
    public class BookingProcessor : BackgroundService
    {
        private readonly BookingQueueService _queue;
        private readonly IServiceScopeFactory _scopeFactory;

        public BookingProcessor(BookingQueueService queue, IServiceScopeFactory scopeFactory)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var request in _queue.Reader.ReadAllAsync(stoppingToken))
            {
                using var scope = _scopeFactory.CreateScope();
                var _context = scope.ServiceProvider.GetRequiredService<HospitalManagementContext>();
                var _emailService = scope.ServiceProvider.GetRequiredService<EmailService>();

                var model = request.Model;

                var patientIdClaim = request.User?.FindFirst("PatientID");
                if (patientIdClaim == null || !int.TryParse(patientIdClaim.Value, out var patientId))
                {
                    continue;
                }

                var patient = await _context.Patients.FindAsync(patientId);
                if (patient == null)
                {
                    continue;
                }
                var excludedStatuses = new[] {
                    AppConstants.AppointmentStatus.Rejected,
                    AppConstants.AppointmentStatus.Failed,
                    AppConstants.AppointmentStatus.Expired
                };
                // Trùng lịch bác sĩ
                bool doctorConflict = await _context.Appointments.AnyAsync(a =>
                    a.DoctorId == model.SelectedDoctorId &&
                    a.Date == model.AppointmentDate &&
                    a.SlotId == model.SelectedSlotId &&
                    !excludedStatuses.Contains(a.Status));

                if (doctorConflict)
                {
                    var failedAppointment = new Appointment
                    {
                        PatientId = patientId,
                        DoctorId = model.SelectedDoctorId,
                        ServiceId = model.SelectedServiceId,
                        PackageId = model.SelectedPackageId,
                        SlotId = model.SelectedSlotId,
                        Date = model.AppointmentDate,
                        Status = "Failed",
                        Note = "Bác sĩ đã có lịch trong khung giờ này."
                    };
                    _context.Appointments.Add(failedAppointment);
                    await _context.SaveChangesAsync();
                    // Gửi email báo lỗi cho bệnh nhân
                    var body = EmailBuilder.BuildFailedAppointmentEmail(failedAppointment, "Bác sĩ đã có lịch trong khung giờ này. Vui lòng chọn khung giờ khác.");
                    await _emailService.SendEmailAsync(patient.Email, "Đặt lịch không thành công", body);
                    continue;
                }

                // Trùng lịch của chính bệnh nhân
                bool patientConflict = await _context.Appointments.AnyAsync(a =>
                    a.PatientId == patientId &&
                    a.Date == model.AppointmentDate &&
                    a.SlotId == model.SelectedSlotId &&
                    !excludedStatuses.Contains(a.Status));

                if (patientConflict)
                {
                    var failedAppointment = new Appointment
                    {
                        PatientId = patientId,
                        DoctorId = model.SelectedDoctorId,
                        ServiceId = model.SelectedServiceId,
                        PackageId = model.SelectedPackageId,
                        SlotId = model.SelectedSlotId,
                        Date = model.AppointmentDate,
                        Status = "Failed",
                        Note = "Bạn đã đặt lịch trong khung giờ này."
                    };
                    _context.Appointments.Add(failedAppointment);
                    await _context.SaveChangesAsync();
                    // Gửi email báo lỗi cho bệnh nhân
                    var body = EmailBuilder.BuildFailedAppointmentEmail(failedAppointment, "Bạn đã đặt lịch trong khung giờ này. Vui lòng chọn khung giờ khác.");
                    await _emailService.SendEmailAsync(patient.Email, "Đặt lịch không thành công", body);
                    continue;
                }

                // Tạo cuộc hẹn thành công
                var appointment = new Appointment
                {
                    PatientId = patientId,
                    DoctorId = model.SelectedDoctorId,
                    ServiceId = model.SelectedServiceId,
                    PackageId = model.SelectedPackageId,
                    SlotId = model.SelectedSlotId,
                    Date = model.AppointmentDate,
                    Status = "Pending",
                    Note = model.Note,
                };

                try
                {
                    _context.Appointments.Add(appointment);
                    await _context.SaveChangesAsync();

                    var saved = await _context.Appointments
                        .Include(a => a.Doctor).Include(a => a.Service)
                        .Include(a => a.Package).Include(a => a.Patient)
                        .Include(a => a.Slot)
                        .FirstOrDefaultAsync(a => a.AppointmentId == appointment.AppointmentId);

                    if (saved != null)
                    {
                        var body = EmailBuilder.BuildPendingAppointmentEmail(saved);
                        await _emailService.SendEmailAsync(patient.Email, "Đặt lịch thành công - Chờ duyệt", body);
                    }
                }
                catch (Exception ex)
                {
                    appointment.Status = "Failed";
                    appointment.Note = $"Lỗi trong quá trình xử lý: {ex.Message}";
                    _context.Update(appointment);
                    await _context.SaveChangesAsync();
                }
            }
        }


    }

}
