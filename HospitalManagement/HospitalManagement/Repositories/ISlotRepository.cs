using HospitalManagement.Models;
namespace HospitalManagement.Repositories
{
    public interface ISlotRepository
    {
        Task<List<Slot>> GetAllSlotsAsync();
    }
}
