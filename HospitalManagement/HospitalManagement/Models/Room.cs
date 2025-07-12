using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HospitalManagement.Models;

public partial class Room
{
    public int RoomId { get; set; }

    [Required(ErrorMessage = "Tên phòng không được để trống")]
    [RegularExpression(@"^[A-Z][0-9]{3,4}$", ErrorMessage = "Tên phòng phải có dạng A101 hoặc A1001")]
    public string RoomName { get; set; } = null!;

    public string? Status { get; set; }

    [Required(ErrorMessage = "Loại phòng không được để trống")]
    public string RoomType { get; set; } = null!;

    public virtual ICollection<ScheduleChangeRequest> ScheduleChangeRequests { get; set; } = new List<ScheduleChangeRequest>();

    public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();

    public virtual ICollection<Tracking> Trackings { get; set; } = new List<Tracking>();
    
    public string GetFullRoomStatus()
    {
        if(this.Status == "Active")
        {
            return "Hoạt động";
        }
        else
        {
            return "Bảo trì";
        }
    }
}
