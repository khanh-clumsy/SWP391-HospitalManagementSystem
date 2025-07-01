using System.Security.Claims;

namespace HospitalManagement.ViewModels.Booking
{
    public class BookingRequest
    {
        public BookingByDoctorViewModel Model { get; set; }
        public ClaimsPrincipal User { get; set; }
    }

}
