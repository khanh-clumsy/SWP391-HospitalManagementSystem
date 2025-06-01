using HospitalManagement.Models;
namespace HospitalManagement.ViewModels
{
    public class AccountListViewModel
    {
        public List<Patient> Patients { get; set; }
        public List<Doctor> Doctors { get; set; }
        public List<Staff> Staffs { get; set; }
        public string AccountType { get; set; }
        

    }
}
