using HospitalManagement.Data;
using HospitalManagement.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace HospitalManagement.Services
{
    public class InvoiceService
    {
        private readonly HospitalManagementContext _context;

        public InvoiceService(HospitalManagementContext context)
        {
            _context = context;
        }

        public async Task CreateInvoiceForAppointmentAsync(Appointment appointment)
        {
            var id = appointment.AppointmentId;

            // Đã có invoice cho Service hoặc Package?
            bool hasInvoice = await _context.InvoiceDetails.AnyAsync(i =>
                i.AppointmentId == id &&
                (i.ItemType == "Service" || i.ItemType == "Package"));

            if (hasInvoice) return;

            string itemType = null;
            int itemId = 0;
            string itemName = "";
            decimal unitPrice = 0;

            if (appointment.ServiceId != null)
            {
                itemType = "Service";
                itemId = appointment.ServiceId.Value;
                itemName = appointment.Service?.ServiceType ?? "";
                unitPrice = appointment.Service?.ServicePrice ?? 0;
            }
            else if (appointment.PackageId != null)
            {
                itemType = "Package";
                itemId = appointment.PackageId.Value;
                itemName = appointment.Package?.PackageName ?? "";
                unitPrice = appointment.Package?.FinalPrice ?? 0;
            }

            if (itemType != null)
            {
                var invoice = new InvoiceDetail
                {
                    AppointmentId = id,
                    ItemType = itemType,
                    ItemId = itemId,
                    ItemName = itemName,
                    UnitPrice = unitPrice,
                    PaymentStatus = "Pending",
                    CreatedAt = DateTime.Now
                };

                _context.InvoiceDetails.Add(invoice);
                await _context.SaveChangesAsync();
            }
        }

    }
}
