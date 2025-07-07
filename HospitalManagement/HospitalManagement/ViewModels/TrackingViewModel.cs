namespace HospitalManagement.ViewModels
{
    public class TrackingViewModel
    {
        public int TestRecordID { get; set; }
        public int TestID{ get; set; }
        public string? TestName { get; set; }
        public string? TestStatus { get; set; }

        public int RoomID { get; set; }
        public string RoomName { get; set; } = "";
        public string RoomType { get; set; } = "";

        public string Status { get; set; } = "";
    }
}
