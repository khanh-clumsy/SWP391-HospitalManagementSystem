using HospitalManagement.Models;
using HospitalManagement.ViewModels;

namespace HospitalManagement.Repositories
{
    public interface IFeedbackRepository
    {
        Task<bool> HasCompletedAppointmentWithPackageAsync(int patientId, int packageId);
        Task<bool> HasCompletedAppointmentWithServiceAsync(int patientId, int serviceId);
        Task<bool> PackageExistsAsync(int packageId);
        Task<bool> ServiceExistsAsync(int serviceId);
        Task<int> CountFeedbackAsync(string? name, int? rating, DateOnly? date);
        Task<List<FeedbackManageViewModel>> SearchFeedbackAsync(string? name, int? rating, DateOnly? date, int page, int pageSize);
        Task<List<Feedback>> GetSpecialFeedbacksAsync();
    }
}
