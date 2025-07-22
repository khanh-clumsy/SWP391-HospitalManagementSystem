
namespace HospitalManagement.ViewModels;
public class NewsViewModel
{
    public int NewsId { get; set; }
    public string Title { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Thumbnail { get; set; }
    public string AuthorName { get; set; }
    public int? DoctorId { get; set; }
    public int? StaffId { get; set; }
}