using HospitalManagement.ViewModels;
using Microsoft.EntityFrameworkCore;
namespace HospitalManagement.Repositories
{
    public interface IScheduleChangeRepository
    {
        Task<List<ScheduleRequestViewModel>> SearchAsync(string type, int page, int pageSize);
        Task<int> CountAsync(string type);
    }
}
