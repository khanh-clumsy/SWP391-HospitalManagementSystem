namespace HospitalManagement.Helpers
{
    public static class AppConstants
    {
        public static class Roles
        {
            public const string Patient = "Patient";
            public const string Doctor = "Doctor";
            public const string Receptionist = "Receptionist";
            public const string Cashier = "Cashier";
            public const string Admin = "Admin";
            public const string TestDoctor = "TestDoctor";
            public const string Sales = "Sales";
        }

        public static class ClaimTypes
        {
            public const string StaffId = "StaffID";
            public const string DoctorId = "DoctorID";
            public const string PatientId = "PatientID";
        }

        public static class RoomTypes
        {
            public const string Clinic = "Phòng khám";
            public const string Lab = "Xét nghiệm";
            public const string Imaging = "Chẩn đoán hình ảnh";
            public const string Ultrasound = "Siêu âm";
            public const string Endoscopy = "Nội soi";
            public const string Cashier = "Thu ngân";
            public const string Other = "Khác";
        }

        public static class AppointmentStatus
        {
            public const string Pending = "Pending";
            public const string Confirmed = "Confirmed";
            public const string Ongoing = "Ongoing";
            public const string Completed = "Completed";
            public const string Rejected = "Rejected";
            public const string Failed = "Failed";
            public const string Cancelled = "Cancelled";
            public const string Expired = "Expired";
        }

        public static class AppointmentActions
        {
            public const string Accept = "Accept";
            public const string Reject = "Reject";
        }

        public static class FilterTypes
        {
            public const string All = "All";
            public const string Today = "Today";
            public const string Ongoing = "Ongoing";
            public const string Completed = "Completed";
        }

        public static class PaymentStatus
        {
            public const string Unpaid = "Unpaid";
            public const string Paid = "Paid";
            public const string Pending = "Pending";
            public const string Failed = "Failed";
            public const string Canceled = "Cancelled";
        }

        public static class TestStatus
        {
            public const string WaitingForPayment = "Waiting for payment";
            public const string Paid = "Paid";
            public const string Pending = "Pending";
            public const string Ongoing = "Ongoing";
            public const string Failed = "Failed";
            public const string Canceled = "Cancelled";
            public const string Completed = "Completed";
        }

        public static class RoomStatus
        {
            public const string Active = "Active";
            public const string Maintain = "Maintain";
        }

        public static class Messages
        {
            public static class Appointment
            {
                public const string NotFound = "Không tìm thấy cuộc hẹn.";
                public const string CreateSuccess = "Đặt lịch hẹn thành công!";
                public const string CreateFail = "Đặt lịch hẹn không thành công.";
                public const string ApproveSuccess = "Duyệt cuộc hẹn thành công!";
                public const string ApproveFail = "Duyệt cuộc hẹn không thành công.";
                public const string AlreadyExists = "Bạn đã có một cuộc hẹn trong khung giờ này!";
                public const string InvalidData = "Dữ liệu không hợp lệ cho cuộc hẹn.";
                public const string NoDoctorAssigned = "Cuộc hẹn chưa được chỉ định bác sĩ! Vui lòng chỉ định bác sĩ!";
                public const string AlreadyApproved = "Cuộc hẹn đã được duyệt.";
                public const string Rejected = "Cuộc hẹn đã bị từ chối.";
                public const string Completed = "Cuộc hẹn đã hoàn thành.";
                public const string Failed = "Cuộc hẹn thất bại.";
                public const string InvalidAction = "Thao tác không hợp lệ.";
                public const string InvalidServiceOrPackage = "Chọn dịch vụ khám cơ bản hoặc gói khám chưa hợp lệ!";
                public const string PendingEmailSubject = "Đặt lịch hẹn thành công - Đang chờ duyệt";
                public const string ConfirmedEmailSubject = "Lịch hẹn đã được xác nhận";
                public const string RejectedEmailSubject = "Lịch hẹn đã bị từ chối";
                public const string Processing = "Hệ thống đang xử lý đặt lịch của bạn. Vui lòng kiểm tra lịch hẹn sau vài phút.";
                public const string CanNotStartMedicalExam = "Cuộc hẹn này không thể bắt đầu!";
                public const string CanNotFindDoctorSchedule = "Không tìm thấy lịch làm việc của bác sĩ!";
                public const string PatientHasOngoingAppointment = "Bệnh nhân đã có cuộc hẹn đang diễn ra. Không thể bắt đầu cuộc hẹn mới.";
                public const string PatientHasCompletedAppointment = "Bệnh nhân đã có cuộc hẹn đã hoàn thành. Không thể bắt đầu cuộc hẹn mới.";
                public const string AppointmentAlreadyCompletdCanNotAssignTest = "Cuộc hẹn đã hoàn thành khám, không thể chỉ định thêm xét nghiệm.";
                public const string NoPermission = "Bạn không có quyền truy cập vào cuộc hẹn này!";
                public const string CancelSuccessful = "Đã hủy lịch khám thành công!";
                public const string OnlyAppointmentOngoingCanAccess = "Chỉ được thao tác với cuộc hẹn đang diễn ra.";

            }

            public static class Tracking
            {
                public const string StartAppointmentProcessSuccess = "Quy trình khám đã được bắt đầu thành công.";
                public const string SaveExaminationSuccess = "Thông tin khám đã được lưu thành công.";
                public const string SubmitExaminationSuccess = "Khám bệnh đã được hoàn tất thành công.";
                public const string PatientHasOngoingTest = "Bệnh nhân đang thực hiện xét nghiệm. Vui lòng đợi kết quả trước khi chỉ định thêm.";
                public const string NoTrackingDataFound = "Không tìm thấy dữ liệu theo dõi.";
                public const string PleaseGoToReceptionForPayment = "Bạn hãy tới lễ tân để thanh toán";
                public const string PleaseProceedToTestRoom = "Vui lòng tiến hành đến phòng xét nghiệm";
                public const string AllStepsCompletedInCurrentBatch = "Tất cả các bước đã hoàn thành trong batch hiện tại.";
            }

            public static class Doctor
            {
                public const string NotFound = "Không tìm thấy bác sĩ!";
                public const string DoctorScheduleNotFound = "Bác sĩ không có lịch làm việc trong khung giờ này!";
                public const string DoctorAlreadyHasAppointment = "Bác sĩ đã có cuộc hẹn khác trong khung giờ này.";
                public const string AssignSuccess = "Chỉ định bác sĩ thành công.";
            }

            public static class User
            {
                public const string NotFound = "Không tìm thấy người dùng.";
                public const string PhoneInvalid = "Số điện thoại không hợp lệ.";
                public const string PhoneUsed = "Số điện thoại đã được sử dụng.";
                public const string EmailUsed = "Email đã được sử dụng.";
                public const string AccountInactive = "Tài khoản chưa được kích hoạt.";
                public const string CreateSuccess = "Tạo tài khoản thành công!";
                public const string CreateFail = "Tạo tài khoản thất bại!";
                public const string UpdateSuccess = "Cập nhật tài khoản thành công!";
                public const string UpdateFail = "Cập nhật tài khoản thất bại!";
                public const string PatientNotFound = "Không tìm thấy bệnh nhân!";
                public const string UpdateDOBSuccess = "Cập nhật ngày sinh thành công!";
                public const string PhoneRequired = "Vui lòng cập nhật số điện thoại trước khi đặt cuộc hẹn!";
                public const string NewAccountEmailSubject = "✅ Fmec System - Tài khoản mới";
            }

            public static class Room
            {
                public const string NotFound = "Phòng không tồn tại.";
                public const string CreateSuccess = "Tạo phòng thành công!";
                public const string CreateFail = "Tạo phòng thất bại!";
                public const string InvalidRoomType = "Không tìm thấy loại phòng cho xét nghiệm này!";
            }

            public static class Test
            {
                public const string NotFound = "Xét nghiệm không tồn tại.";
                public const string AlreadyAssigned = "Test này đã được chỉ định cho phòng này.";
                public const string AssignSuccess = "Chỉ định xét nghiệm thành công!";
                public const string AssignFail = "Chỉ định xét nghiệm thất bại!";
                public const string NotCompleted = "Vui lòng hoàn thành tất cả xét nghiệm trước khi kết thúc khám bệnh.";
                public const string TestRecordNotFound = "Không tìm thấy xét nghiệm.";
                public const string ResultFileRequired = "Chưa cập nhật kết quả";
                public const string SaveResultSuccess = "Lưu kết quả xét nghiệm thành công.";
            }

            public static class Package
            {
                public const string NotFound = "Không tìm thấy gói khám!";
                public const string CreateSuccess = "Tạo gói khám thành công!";
                public const string CreateFail = "Tạo gói khám thất bại!";
                public const string UpdateFail = "Cập nhật gói khám thất bại!";
            }

            public static class Auth
            {
                public const string LoginSuccess = "Đăng nhập thành công!";
                public const string LoginFail = "Đăng nhập thất bại!";
                public const string RegisterSuccess = "Đăng ký thành công!";
                public const string RegisterFail = "Đăng ký thất bại!";
                public const string LogoutSuccess = "Đăng xuất thành công!";
                public const string SessionExpired = "Phiên đăng nhập đã hết hạn.";
                public const string InvalidVerificationCode = "Mã xác thực không đúng.";
            }

            public static class General
            {
                public const string NoPermission = "Bạn không có quyền thực hiện thao tác này.";
                public const string NotFound = "Không tìm thấy dữ liệu!";
                public const string SuccessDelete = "Xóa thành công!";
                public const string FailDelete = "Xóa thất bại!";
                public const string UnknownError = "Đã xảy ra lỗi không xác định!";
                public const string SaveSuccess = "Lưu thông tin thành công!";
                public const string Undefined = "Không xác định.";
                public const string InvalidDate = "Ngày không hợp lệ!";
                public const string ModelStateInvalid = "Dữ liệu không hợp lệ!";
                public const string InvalidData = "Không hợp lệ!";
                public const string DepartmentRequired = "Department name is required.";
            }
            public static class Service
            {
                public const string CreateSuccess = "Thêm dịch vụ thành công!";
                public const string UpdateSuccess = "Cập nhật dịch vụ thành công!";
                public const string SoftDeleteSuccess = "Dịch vụ đã được ẩn!";
                public const string RestoreSuccess = "Khôi phục dịch vụ thành công!";
            }
        }

        public static class InvoiceItemTypes
        {
            public const string Service = "Service";
            public const string Package = "Package";
            public const string Test = "Test";
            public const string Medicine = "Medicine";
        }


    }
}