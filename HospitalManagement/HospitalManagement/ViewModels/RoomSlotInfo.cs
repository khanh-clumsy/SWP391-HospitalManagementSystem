namespace HospitalManagement.ViewModels
{
    public class RoomSlotInfo
    {
        public int RoomId { get; set; }
        public string RoomName { get; set; } = "";
        public int SlotId { get; set; }

        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }

        public string SlotTime { get; set; } = "";
    }

}
