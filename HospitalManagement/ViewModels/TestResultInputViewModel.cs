namespace HospitalManagement.ViewModels
{
    public class TestResultInputViewModel
    {
        public int TestRecordID { get; set; }

        public int TestID { get; set; }

        public string TestName { get; set; } = "";

        // Bệnh nhân
        public int PatientID { get; set; }
        public string PatientFullName { get; set; } = "";
        public string Gender { get; set; } = "";
        public DateTime? DOB { get; set; }

        // Ghi chú bác sĩ
        public string Note { get; set; } = "";

        //Thực hiện bởi bác sỹ :
        public int DoctorID { get; set; }

        public string DoctorName { get; set; } = "";
        // File upload

        public string? ResultFileName { get; set; } = null;
        public IFormFile ResultFile { get; set; } = null!;

        // Kết quả nếu cần (optional)
        public string Result { get; set; } = "";
    }
}
