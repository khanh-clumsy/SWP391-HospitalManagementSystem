using HospitalManagement.Models;

namespace HospitalManagement.ViewModels
{
    public class ViewConsultationsViewModel
    {
        public List<Consultant>? Consultants { get; set; } = new List<Consultant>();

        public DateTime? DateFilter { get; set; }
        public string? StatusFilter { get; set; }
    }
}
