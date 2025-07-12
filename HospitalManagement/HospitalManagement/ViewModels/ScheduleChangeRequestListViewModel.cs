using X.PagedList;

namespace HospitalManagement.ViewModels
{
    public class ScheduleChangeRequestListViewModel
    {
        public StaticPagedList<ScheduleRequestViewModel> Requests { get; set; }
        public string ViewType { get; set; } = "Pending"; // hoặc "Completed"
    }
}
