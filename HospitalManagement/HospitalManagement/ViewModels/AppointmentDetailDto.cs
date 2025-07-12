namespace HospitalManagement.ViewModels
{
    public class AppointmentDetailDto
    {
        public int AppointmentId { get; set; }
        public string PatientName { get; set; }
        public string DoctorName { get; set; }
        public string? ServiceType { get; set; }
        public string? PackageName { get; set; }
        public string? StaffRoleName { get; set; }
        public int? CreatedByStaffId { get; set; }
        public string? StaffName { get; set; }
        public decimal? TotalPrice { get; set; }
        public DateOnly Date { get; set; }
    }
}
