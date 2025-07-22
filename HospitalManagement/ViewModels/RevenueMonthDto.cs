using System.Globalization;

namespace HospitalManagement.ViewModels
{
    public class RevenueMonthDto
    {
        public string Month { get; set; }
        public decimal TotalRevenue { get; set; }
        public string MonthDisplay =>
        DateTime.ParseExact(Month, "yyyy-MM", CultureInfo.InvariantCulture)
                 .ToString("MM/yyyy");
    }
}
