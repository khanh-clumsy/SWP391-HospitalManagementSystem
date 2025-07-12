namespace HospitalManagement.ViewModels
{
    public class TestRecordViewModel
    {
        public int TestRecordId { get; set; }
        public int TestId { get; set; }
        public string TestName { get; set; }

        public string TestStatus { get; set; }

        public int? RoomId { get; set; }
        public string RoomName { get; set; }
        public string RoomType { get; set; }

        public bool IsAssigned => RoomId != null;
    }
}
