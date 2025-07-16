using HospitalManagement.ViewModels;

namespace HospitalManagement.Repositories
{
    public interface IInvoiceRepository
    {
        Task<List<RevenueMonthDto>> GetMonthlyRevenueStatisticsAsync(int year);
        Task<List<int>> GetDistinctYearOfRevenue();
        Task<List<InvoiceDetailDto>> GetInvoiceDetailsByMonthAsync(string month);
    }
}