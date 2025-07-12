using System.ComponentModel.DataAnnotations;

namespace HospitalManagement.ViewModels
{
    public class FeedbackCreateViewModel
    {
        public int? ServiceId { get; set; }
        public int? PackageId { get; set; }

        [Range(1, 5, ErrorMessage = "Số sao đánh giá phải từ 1 đến 5")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "Vui lòng điền nhận xét")]
        public string Comment { get; set; }

        // Ẩn trong view, dùng nội bộ
        public bool IsSpecial { get; set; } = false;
    }
}
