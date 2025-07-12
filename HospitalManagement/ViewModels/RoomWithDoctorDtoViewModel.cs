namespace HospitalManagement.ViewModels
{
    public class RoomWithDoctorDtoViewModel
    {
        public int RoomId { get; set; }
        public string RoomName { get; set; }
        public string RoomType { get; set; }
        public string? Status { get; set; }

        public int? DoctorID { get; set; }
        public string? DoctorName { get; set; }
        public string GetFullRoomStatus()
        {
            if (this.Status == "Active")
            {
                return "Hoạt động";
            }
            else
            {
                return "Bảo trì";
            }
        }
    }   
}
