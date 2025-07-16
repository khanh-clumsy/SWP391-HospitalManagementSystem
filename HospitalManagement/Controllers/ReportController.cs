using HospitalManagement.Data;
using HospitalManagement.Repositories;
using HospitalManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using X.PagedList;

namespace HospitalManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportController : Controller
    {
        private readonly IInvoiceRepository _invoiceRepo;
        private readonly IAppointmentRepository _appointmentRepo;
        public ReportController(IInvoiceRepository invoiceRepo, IAppointmentRepository appointmentRepo)
        {
            _invoiceRepo = invoiceRepo;
            _appointmentRepo = appointmentRepo;
        }

        public async Task<IActionResult> RevenueStatistics(int? year)
        {
            var years = await _invoiceRepo.GetDistinctYearOfRevenue();

            ViewBag.Years = years;
            int selectedyear = year ?? DateTime.Now.Year;
            ViewBag.SelectedYear = selectedyear;
            try
            {
                var revenueData = await _invoiceRepo.GetMonthlyRevenueStatisticsAsync(selectedyear);
                return View(revenueData); // truyền model mạnh kiểu
            }
            catch (Exception ex)
            {
                TempData["error"] = "Lỗi khi truy xuất dữ liệu thống kê doanh thu.";
                return RedirectToAction("RevenueStatistics");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ExportRevenueToExcel(int? year)
        {
            int selectedYear = year ?? DateTime.Now.Year;

            var revenueData = await _invoiceRepo.GetMonthlyRevenueStatisticsAsync(selectedYear);

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add($"Doanh thu {selectedYear}");

                // Tiêu đề
                worksheet.Cells["A1"].Value = $"BÁO CÁO DOANH THU NĂM {selectedYear}";
                worksheet.Cells["A1:D1"].Merge = true;
                worksheet.Cells["A1"].Style.Font.Size = 16;
                worksheet.Cells["A1"].Style.Font.Bold = true;
                worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // Header
                worksheet.Cells["A3"].Value = "Tháng";
                worksheet.Cells["B3"].Value = "Doanh thu (VND)";
                worksheet.Cells["A3:B3"].Style.Font.Bold = true;

                int row = 4;
                foreach (var item in revenueData)
                {
                    var month = DateTime.ParseExact(item.Month, "yyyy-MM", null).ToString("MM/yyyy");
                    worksheet.Cells[row, 1].Value = month;
                    worksheet.Cells[row, 2].Value = item.TotalRevenue;
                    worksheet.Cells[row, 2].Style.Numberformat.Format = "#,##0";
                    row++;
                }

                // Tổng cộng
                worksheet.Cells[row, 1].Value = "Tổng cộng";
                worksheet.Cells[row, 2].Formula = $"SUM(B4:B{row - 1})";
                worksheet.Cells[row, 2].Style.Font.Bold = true;
                worksheet.Cells[row, 2].Style.Numberformat.Format = "#,##0";

                worksheet.Cells[$"A3:B{row}"].AutoFitColumns();

                var stream = new MemoryStream(package.GetAsByteArray());

                string fileName = $"DoanhThu_{selectedYear}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        public async Task<IActionResult> MonthlyInvoiceDetails(string month, int? page)
        {
            if (string.IsNullOrEmpty(month)) return RedirectToAction("RevenueStatistics");
            var allMonthlyDetails = await _invoiceRepo.GetInvoiceDetailsByMonthAsync(month);
            ViewBag.AllMonthlyDetails = allMonthlyDetails;
            int pageSize = 10;
            int pageNumber = page ?? 1;

            var allDetails = await _invoiceRepo.GetInvoiceDetailsByMonthAsync(month);
            var pagedDetails = allDetails
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var pagedList = new StaticPagedList<InvoiceDetailDto>(pagedDetails, pageNumber, pageSize, allDetails.Count);

            ViewBag.Month = month;
            ViewBag.MonthDisplay = DateTime.ParseExact(month, "yyyy-MM", null).ToString("MM/yyyy");

            return View(pagedList);
        }

        [HttpPost]
        public async Task<IActionResult> ExportMonthlyInvoiceToExcel(string month)
        {
            var data = await _invoiceRepo.GetInvoiceDetailsByMonthAsync(month);
            var displayMonth = DateTime.ParseExact(month, "yyyy-MM", null).ToString("MM/yyyy");

            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add($"ChiTietDoanhThu_{displayMonth}");

                // Header
                ws.Cells["A1"].Value = $"CHI TIẾT DOANH THU THÁNG {displayMonth}";
                ws.Cells["A1:F1"].Merge = true;
                ws.Cells["A1"].Style.Font.Bold = true;
                ws.Cells["A1"].Style.Font.Size = 16;
                ws.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // Column headers
                ws.Cells["A3"].Value = "Tên bệnh nhân";
                ws.Cells["B3"].Value = "Loại dịch vụ";
                ws.Cells["C3"].Value = "Tên dịch vụ";
                ws.Cells["D3"].Value = "Đơn giá";
                ws.Cells["E3"].Value = "Trạng thái";
                ws.Cells["F3"].Value = "Thời gian thanh toán";

                ws.Cells["A3:F3"].Style.Font.Bold = true;

                int row = 4;
                foreach (var item in data)
                {
                    ws.Cells[row, 1].Value = item.PatientName;
                    ws.Cells[row, 2].Value = item.ItemType;
                    ws.Cells[row, 3].Value = item.ItemName;
                    ws.Cells[row, 4].Value = item.UnitPrice;
                    ws.Cells[row, 4].Style.Numberformat.Format = "#,##0";
                    ws.Cells[row, 5].Value = item.PaymentStatus;
                    ws.Cells[row, 6].Value = item.PaymentTime?.ToString("dd/MM/yyyy");

                    row++;
                }

                ws.Cells[$"A3:F{row - 1}"].AutoFitColumns();

                var stream = new MemoryStream(package.GetAsByteArray());
                var fileName = $"ChiTietDoanhThu_{displayMonth}.xlsx";

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }
        public async Task<IActionResult> AppointmentUsage(int? year)
        {
            int selectedYear = year ?? DateTime.Now.Year;

            var stats = await _appointmentRepo.GetMonthlyUsageStatsAsync(selectedYear);
            var years = await _appointmentRepo.GetAvailableYearsWithCompletedAppointmentsAsync();

            var viewModel = new AppointmentUsageStatsViewModel
            {
                Year = selectedYear,
                MonthlyStats = stats,
                AvailableYears = years
            };

            return View(viewModel);
        }
        public async Task<IActionResult> MonthlyDetail(int year, int month, int? page)
        {
            int pageSize = 10;
            int pageNumber = page ?? 1;

            var (details, total) = await _appointmentRepo
                .GetMonthlyAppointmentDetailsAsync(year, month, pageNumber, pageSize);

            var pagedDetails = new StaticPagedList<AppointmentDetailDto>(details, pageNumber, pageSize, total);

            // Dữ liệu aggregate cho biểu đồ:
            var allDetails = await _appointmentRepo.GetMonthlyAppointmentDetailsAsync(year, month, 1, int.MaxValue);
            var fullList = allDetails.Details;

            ViewBag.ServiceGroups = fullList
                .GroupBy(x => x.ServiceType ?? "N/A")
                .Where(g => g.Key != "N/A")
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .ToList();

            ViewBag.PackageGroups = fullList
                .GroupBy(x => x.PackageName ?? "N/A")
                .Where(g => g.Key != "N/A")
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .ToList();

            var staffSalesCount = fullList
                .Count(x => x.CreatedByStaffId != null && x.StaffRoleName == "Sales");
            var staffTotal = fullList.Count;

            ViewBag.SalesStats = new { SalesCount = staffSalesCount, Total = staffTotal };

            ViewBag.Year = year;
            ViewBag.Month = month;

            return View(pagedDetails);
        }

        [HttpPost]
        public async Task<IActionResult> ExportMonthlyAppointmentsToExcel(int year, int month)
        {
            var (appointments, _) = await _appointmentRepo.GetMonthlyAppointmentDetailsAsync(year, month, 1, int.MaxValue);

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add($"Appointments_{month:D2}_{year}");

            // 🔹 A1: Tiêu đề chính
            string title = $"Chi tiết cuộc hẹn tháng {month:D2}/{year}";
            ws.Cells["A1"].Value = title;
            ws.Cells["A1:F1"].Merge = true;
            ws.Cells["A1"].Style.Font.Bold = true;
            ws.Cells["A1"].Style.Font.Size = 16;
            ws.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // 🔹 Header ở dòng 3
            string[] headers = { "ID", "Bệnh nhân", "Bác sĩ", "Dịch vụ", "Gói khám", "Nhân viên tư vấn", "Tiền (VND)", "Ngày" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cells[3, i + 1];
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
            }

            // 🔹 Dữ liệu bắt đầu từ dòng 4
            int startRow = 4;
            for (int i = 0; i < appointments.Count(); i++)
            {
                var a = appointments[i];
                int row = startRow + i;

                ws.Cells[row, 1].Value = a.AppointmentId;
                ws.Cells[row, 2].Value = a.PatientName;
                ws.Cells[row, 3].Value = a.DoctorName;
                ws.Cells[row, 4].Value = a.ServiceType ?? "N/A";
                ws.Cells[row, 5].Value = a.PackageName ?? "N/A";
                ws.Cells[row, 6].Value = a.StaffRoleName == "Sales" ? a.StaffName : "N/A";
                ws.Cells[row, 7].Value = a.TotalPrice.HasValue ? (double?)a.TotalPrice.Value : null;
                ws.Cells[row, 8].Value = a.Date.ToString("dd/MM/yyyy");
            }

            ws.Cells[ws.Dimension.Address].AutoFitColumns();

            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"Appointments_{month:D2}_{year}.xlsx";
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

    }
}
