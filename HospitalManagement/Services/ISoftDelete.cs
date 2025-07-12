namespace HospitalManagement.Services
{
    public interface ISoftDelete
    {
        bool IsDeleted { get; set; }
    }
}
