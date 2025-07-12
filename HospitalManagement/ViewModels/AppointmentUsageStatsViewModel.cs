namespace HospitalManagement.ViewModels
{
    public class AppointmentUsageStatsViewModel
    {
        public int Year { get; set; }

        public List<MonthlyAppointmentUsageDto> MonthlyStats { get; set; } = new();

        public List<int> AvailableYears { get; set; } = new();
    }
}
