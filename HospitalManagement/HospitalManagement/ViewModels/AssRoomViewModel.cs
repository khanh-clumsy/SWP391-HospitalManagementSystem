using Microsoft.AspNetCore.Mvc;
using HospitalManagement.Models;
namespace HospitalManagement.ViewModels
{
    public class RoomWithTestCountViewModel
    {
        public int RoomId { get; set; }
        public string RoomName { get; set; }
        public int TestCount { get; set; } // số test đang chờ tại phòng
    }
}
