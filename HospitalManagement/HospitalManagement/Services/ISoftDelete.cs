namespace HospitalManagement.Services
{
    public interface ISoftDelete
    {
        public bool IsDeleted { get; set; }
    }
}
