using HospitalManagement.Models;
namespace HospitalManagement.Repositories
{
    public interface ITestRepository
    {
        IEnumerable<Test> GetAll();
        Test GetById(int id);
        void Add(Test test);
        void Update(Test test);
        void Delete(int id);
        void Save();
        IEnumerable<Test> Search(string name, string sortOrder, decimal? minPrice, decimal? maxPrice);
    }
}

