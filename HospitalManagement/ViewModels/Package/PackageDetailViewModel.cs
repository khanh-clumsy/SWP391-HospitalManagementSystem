using HospitalManagement.Models;

namespace HospitalManagement.ViewModels.Package
{
    public class PackageDetailViewModel
    {
        public PackageViewModel Package { get; set; } = null!;

        public List<Test> Tests { get; set; } = new();

        public int BookingCount { get; set; }
    }
}
