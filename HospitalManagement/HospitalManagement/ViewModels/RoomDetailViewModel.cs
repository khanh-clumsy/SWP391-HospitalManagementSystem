namespace HospitalManagement.ViewModels
{
    public class RoomDetailViewModel
    {
        public int RoomId { get; set; }
        public string RoomName { get; set; }
        public string RoomType { get; set; }
        public string? Status { get; set; }

        public List<RoomScheduleItemViewModel> Schedule { get; set; } = new();
    }

}
