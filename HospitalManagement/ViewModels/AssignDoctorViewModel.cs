namespace HospitalManagement.ViewModels
{
    public class AssignDoctorViewModel
    {
        public int AppointmentId { get; set; }
        public DateOnly AppointmentDate { get; set; }
        public string? SlotTimeText { get; set; }
        public int SlotId { get; set; }
        public List<Models.Doctor> Doctors { get; set; } = new List<Models.Doctor>();

        public int? SelectedDoctorId { get; set; }
    }
}
