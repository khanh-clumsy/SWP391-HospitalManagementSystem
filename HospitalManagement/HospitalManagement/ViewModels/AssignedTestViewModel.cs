namespace HospitalManagement.ViewModels
{
    public class AssignedTestViewModel
    {
        public int TrackingId { get; set; }
        public string PatientName { get; set; }
        public string TestName { get; set; }
        public string RoomName { get; set; }
        public DateTime Time { get; set; }
        public string TestStatus { get; set; }
    }
}
