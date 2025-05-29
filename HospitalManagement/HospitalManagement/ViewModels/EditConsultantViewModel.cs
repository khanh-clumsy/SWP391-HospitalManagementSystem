using System.ComponentModel.DataAnnotations;

namespace HospitalManagement.ViewModels
{
    public class EditConsultantViewModel
    {
        public int ConsultantID { get; set; }

        public string? Name { get; set; }  

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

     
        public DateOnly? RequestedDate { get; set; }

        public string? Consultants { get; set; }

        public int? ServiceID { get; set; }

        public string? Description { get; set; }
    }
}
