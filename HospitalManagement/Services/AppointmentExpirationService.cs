using System;
using System.Threading;
using System.Threading.Tasks;
using HospitalManagement.Data;
using HospitalManagement.Helpers;
using HospitalManagement.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class AppointmentExpirationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AppointmentExpirationService> _logger;

    public AppointmentExpirationService(IServiceProvider serviceProvider, ILogger<AppointmentExpirationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AppointmentExpirationService is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            await CheckExpiredAppointmentsAsync();
            await Task.Delay(TimeSpan.FromMinutes(59), stoppingToken);
           // await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
        _logger.LogInformation("AppointmentExpirationService is stopping.");
    }
    private async Task CheckExpiredAppointmentsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HospitalManagementContext>();
        var now = DateTime.Now;
        var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();

        var appointments = await dbContext.Appointments
            .Include(a => a.Slot)
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Include(a => a.Service)
            .Include(a => a.Package)
            .Include(a => a.CreatedByStaff)
            .Where(a => a.Status == AppConstants.AppointmentStatus.Confirmed)
            .ToListAsync();

        var expiredAppointments = appointments
            .Where(a =>
            {
                // Ghép DateOnly + TimeOnly thành DateTime
                var endDateTime = a.Date.ToDateTime(a.Slot.EndTime);

                return endDateTime < now;
            }).ToList();

        foreach (var appt in expiredAppointments)
        {
            appt.Status = AppConstants.AppointmentStatus.Expired;
            // Gửi email cho bệnh nhân
            if (!string.IsNullOrEmpty(appt.Patient?.Email))
            {
                var subject = "[FMEC] Cuộc hẹn của bạn đã quá hạn";
                var body = EmailBuilder.BuildExpiredAppointmentEmail(appt);
                await emailService.SendEmailAsync(appt.Patient.Email, subject, body);
            }
        }

        if (expiredAppointments.Any())
        {
            await dbContext.SaveChangesAsync();
        }
    }



}
