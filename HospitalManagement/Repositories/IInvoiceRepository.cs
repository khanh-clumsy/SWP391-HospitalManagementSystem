using HospitalManagement.ViewModels;

namespace HospitalManagement.Repositories
{
    public interface IInvoiceRepository
    {
        Task<List<RevenueMonthDto>> GetMonthlyRevenueStatisticsAsync(int year);
        Task<List<int>> GetDistinctYearOfRevenue();
        Task<List<InvoiceDetailDto>> GetInvoiceDetailsByMonthAsync(string month);
        Task<List<InvoiceDetailDto>> GetInvoiceDetailsByDateRangeAsync(DateTime fromDate, DateTime toDate);
        Task<decimal> GetTotalRevenueByDateRangeAsync(DateTime fromDate, DateTime toDate);

    }
}