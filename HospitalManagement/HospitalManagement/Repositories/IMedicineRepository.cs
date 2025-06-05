using HospitalManagement.Models;

namespace HospitalManagement.Repositories
{
    public interface IMedicineRepository
    {
        Task<List<Medicine>> Filter(string? searchName, string? typeFilter);
    }

}
