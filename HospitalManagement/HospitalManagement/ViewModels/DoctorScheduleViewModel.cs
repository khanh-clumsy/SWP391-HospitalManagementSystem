public class DoctorScheduleViewModel
{
    public class ScheduleItem
    {
        public int ScheduleId { get; set; }
        public DateOnly Day { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public int SlotId { get; set; }
        public string RoomName { get; set; }
        public int RoomId { get; set; }
        public int DoctorId { get; set; }
        public string Status { get; set; }
        public string DoctorName {get; set;}

    }
}
