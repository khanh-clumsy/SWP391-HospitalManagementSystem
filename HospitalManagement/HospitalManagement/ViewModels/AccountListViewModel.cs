using HospitalManagement.Models;
using X.PagedList;
namespace HospitalManagement.ViewModels
{
    public class AccountListViewModel
    {
        public StaticPagedList<Patient> Patients { get; set; }
        public StaticPagedList<Doctor> Doctors { get; set; }
        public StaticPagedList<Staff> Staffs { get; set; }
        public string AccountType { get; set; }
        

    }
}
