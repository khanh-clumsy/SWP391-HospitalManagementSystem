using HospitalManagement.Data;
using HospitalManagement.ViewModels;
using Microsoft.EntityFrameworkCore;
namespace HospitalManagement.Repositories
{
    public class InvoiceRepository : IInvoiceRepository
    {
        private readonly HospitalManagementContext _context;
        public InvoiceRepository(HospitalManagementContext context)
        {
            _context = context;
        }

        public async Task<List<int>> GetDistinctYearOfRevenue()
        {
            return await _context.InvoiceDetails
                .Where(i => i.PaymentTime != null)
                .Select(i => i.PaymentTime.Value.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();
        }

        public async Task<List<RevenueMonthDto>> GetMonthlyRevenueStatisticsAsync(int year)
        {
            var invoices = await _context.InvoiceDetails
                .Where(i => i.PaymentStatus == "Paid" && i.PaymentTime != null && i.PaymentTime.Value.Year == year)
                .ToListAsync();

            var result = invoices
                .GroupBy(i => i.PaymentTime.Value.ToString("yyyy-MM"))
                .Select(g => new RevenueMonthDto
                {
                    Month = g.Key,
                    TotalRevenue = g.Sum(x => x.UnitPrice)
                })
                .OrderBy(x => x.Month)
                .ToList();

            return result;
        }
        public async Task<List<InvoiceDetailDto>> GetInvoiceDetailsByMonthAsync(string month)
        {
            var start = DateTime.ParseExact(month, "yyyy-MM", null);
            var end = start.AddMonths(1);

            return await _context.InvoiceDetails
                .Include(i => i.Appointment)
                .ThenInclude(a => a.Patient)
                .Where(i => i.PaymentStatus == "Paid" && i.PaymentTime >= start && i.PaymentTime < end)
                .Select(i => new InvoiceDetailDto
                {
                    PatientName = i.Appointment.Patient.FullName,
                    ItemType = i.ItemType,
                    ItemName = i.ItemName,
                    UnitPrice = i.UnitPrice,
                    PaymentStatus = i.PaymentStatus,
                    PaymentTime = i.PaymentTime
                })
                .ToListAsync();
        }

    }
}