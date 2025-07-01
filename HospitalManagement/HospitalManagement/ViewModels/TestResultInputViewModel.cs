namespace HospitalManagement.ViewModels
{
    public class TestResultInputViewModel
    {
        public int TestListID { get; set; }
        public string TestName { get; set; }

        // Bệnh nhân
        public string PatientFullName { get; set; }
        public string Gender { get; set; }
        public DateTime DOB { get; set; }

        // Ghi chú bác sĩ
        public string Note { get; set; }

        // File upload
        public IFormFile ResultFile { get; set; }

        // Kết quả nếu cần (optional)
        public string Result { get; set; }
    }
}
