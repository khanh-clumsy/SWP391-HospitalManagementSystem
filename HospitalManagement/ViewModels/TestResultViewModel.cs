namespace HospitalManagement.ViewModels
{
    public class TestResultViewModel
    {
        public string PatientFullName { get; set; }
        public string Gender { get; set; }
        public DateTime DOB { get; set; }
        public string TestName { get; set; }
        public string Note { get; set; }
        public string ResultFileName { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

}
