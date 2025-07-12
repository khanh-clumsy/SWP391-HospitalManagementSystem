using Microsoft.AspNetCore.Mvc;
using HospitalManagement.Data;
using HospitalManagement.Models;
using HospitalManagement.ViewModels; 
using HospitalManagement.ViewModels.VnPay; 
using Microsoft.EntityFrameworkCore;
using HospitalManagement.Services;
using HospitalManagement.Services.VnPay;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using System.Text.RegularExpressions;

public class PaymentController : Controller
{
    private readonly HospitalManagementContext _context;
    private readonly IVnPayService _vnPayService;

    private readonly IConfiguration _configuration;

    public PaymentController(HospitalManagementContext context, IVnPayService vnPayService, IConfiguration configuration)
    {
        _context = context;
        _vnPayService = vnPayService;
        _configuration = configuration; // gán vào field

    }

    [HttpGet]
    public async Task<IActionResult> PayStartAppointment(int invoiceId)
    {
        var invoicedetail = await _context.InvoiceDetails
            .Include(x => x.Appointment)
            .FirstOrDefaultAsync(a => a.InvoiceDetailId == invoiceId);


        if (invoicedetail == null)
            return NotFound();

        var paymentModel = new VnPayViewModel
        {
            Name = "Name",
            Amount = invoicedetail.UnitPrice,
            OrderDescription = $"{invoicedetail.ItemType} - {invoicedetail.ItemName}",
            OrderType = "other",
            InvoiceId = invoiceId
        };

        string paymentUrl = _vnPayService.CreatePaymentUrl(paymentModel, HttpContext);
        ViewBag.PaymentUrl = paymentUrl;
        ViewBag.InvoiceDetailId = invoiceId;
        return View(invoicedetail);
    }

    public IActionResult GenerateQr(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return NotFound();

        var decodedUrl = WebUtility.UrlDecode(url);

        using (var qrGenerator = new QRCodeGenerator())
        {
            var qrData = qrGenerator.CreateQrCode(decodedUrl, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new SvgQRCode(qrData);
            var svgImage = qrCode.GetGraphic(5);

            // Trả về SVG (vector, tốt hơn PNG)
            return Content(svgImage, "image/svg+xml");
        }
    }

    [HttpGet]
    public IActionResult PaymentCallbackVnpay() // bank 
    {
        var vnpayData = Request.Query;
        var responseData = new SortedList<string, string>();

        foreach (var key in vnpayData.Keys)
        {
            if (key.StartsWith("vnp_"))
            {
                responseData.Add(key, vnpayData[key]);
            }
        }

        // Lấy mã giao dịch và mã đơn hàng
        string responseCode = responseData["vnp_ResponseCode"];
        string txnRef = responseData["vnp_TxnRef"];

        switch (responseCode)
        {
            case "00":
                // Lấy OrderInfo và trích InvoiceId
                string orderInfo = responseData["vnp_OrderInfo"];
                int invoiceId = 0;

                if (orderInfo.StartsWith("Invoice:"))
                {
                    // Cắt chuỗi trước dấu " -"
                    var match = Regex.Match(orderInfo, @"Invoice:(\d+)");
                    if (match.Success)
                    {
                        invoiceId = int.Parse(match.Groups[1].Value);
                    }
                }

                if (invoiceId > 0)
                {
                    var invoiceDetail = _context.InvoiceDetails
                        .Include(i => i.Appointment)
                        .FirstOrDefault(i => i.InvoiceDetailId == invoiceId);

                    if (invoiceDetail != null)
                    {
                        invoiceDetail.PaymentMethod = "Banking";
                        invoiceDetail.PaymentStatus = "Paid";
                        invoiceDetail.PaymentTime = DateTime.Now;

                        if (invoiceDetail.ItemType == "Test")
                        {
                            var testRecord = _context.TestRecords.FirstOrDefault(tr =>
                                tr.AppointmentId == invoiceDetail.AppointmentId &&
                                tr.TestRecordId == invoiceDetail.ItemId);

                            if (testRecord != null)
                            {
                                testRecord.TestStatus = "Ongoing";
                            }
                        }
                        _context.SaveChanges();

                        ViewBag.PaymentMethod = invoiceDetail.PaymentMethod;
                        ViewBag.PaymentTime = invoiceDetail.PaymentTime;
                    }


                    ViewBag.Message = "Thanh toán thành công.";
                }
                else
                {
                    ViewBag.Message = "Không xác định được hóa đơn cần cập nhật.";
                }

                break;
            case "07":
                ViewBag.Message = "Giao dịch bị nghi ngờ gian lận.";
                break;
            case "09":
                ViewBag.Message = "Tài khoản chưa đăng ký Internet Banking.";
                break;
            case "10":
                ViewBag.Message = "Xác thực giao dịch thất bại (sai OTP).";
                break;
            case "11":
                ViewBag.Message = "Giao dịch hết hạn.";
                break;
            case "12":
                ViewBag.Message = "Dữ liệu gửi đi không hợp lệ.";
                break;
            case "13":
                ViewBag.Message = "Tài khoản bị khóa hoặc không đủ số dư.";
                break;
            case "24":
                ViewBag.Message = "Giao dịch đã bị hủy bởi người dùng.";
                break;
            case "51":
                ViewBag.Message = "Tài khoản không đủ số dư.";
                break;
            case "65":
                ViewBag.Message = "Tài khoản vượt quá hạn mức giao dịch.";
                break;
            case "75":
                ViewBag.Message = "Ngân hàng đang bảo trì.";
                break;
            case "91":
                ViewBag.Message = "Không nhận được phản hồi từ ngân hàng.";
                break;
            case "93":
                ViewBag.Message = "Lỗi hệ thống ngân hàng hoặc VNPAY.";
                break;
            case "94":
                ViewBag.Message = "Giao dịch bị trùng lặp.";
                break;
            case "95":
                ViewBag.Message = "Không tìm thấy giao dịch.";
                break;
            case "97":
                ViewBag.Message = "Lỗi CSRF hoặc tham số không hợp lệ.";
                break;
            case "99":
                ViewBag.Message = "Lỗi không xác định.";
                break;
            default:
                ViewBag.Message = $"Giao dịch thất bại. Mã lỗi: {responseCode}";
                break;
        }

        return View("PaymentResult");
    }

    [HttpPost]
    public async Task<IActionResult> PayAppointmentConfirmed(int invoiceId) // cash
    {
        // Lấy invoice detail chưa thanh toán (Pending) cho cuộc hẹn này
        var invoiceDetail = await _context.InvoiceDetails
            .Where(i => i.InvoiceDetailId == invoiceId)
            .FirstOrDefaultAsync();

        if (invoiceDetail == null)
        {
            TempData["error"] = "Không tìm thấy hóa đơn cần xác nhận.";
            return RedirectToAction("StartAppointmentProcess", "Tracking");
        }

        if (invoiceDetail.ItemType == "Test")
        {
            var testRecord = await _context.TestRecords.FirstOrDefaultAsync(tr =>
                tr.AppointmentId == invoiceDetail.AppointmentId &&
                tr.TestRecordId == invoiceDetail.ItemId);

            if (testRecord != null)
            {
                testRecord.TestStatus = "Ongoing";
            }
        }
        // Cập nhật thông tin thanh toán
        invoiceDetail.PaymentMethod = "Cash";
        invoiceDetail.PaymentStatus = "Paid";
        invoiceDetail.PaymentTime = DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["success"] = "Đã xác nhận thanh toán tiền mặt thành công.";

        if (invoiceDetail.ItemType == "Test")
        {
            return RedirectToAction("ViewInvoiceList", "Tracking");

        }
        else
        {
            return RedirectToAction("StartAppointmentProcess", "Tracking");

        }
    }

    [HttpGet]
    public async Task<IActionResult> TotalInvoiceDetail(int id)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Slot)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.AppointmentId == id);

        var invoiceDetails = await _context.InvoiceDetails
            .Where(i => i.AppointmentId == id)
            .ToListAsync();

        // Load TestRecords theo các item có type là "Test"
        var testRecordIds = invoiceDetails
            .Where(i => i.ItemType == "Test")
            .Select(i => i.ItemId)
            .ToList();

        var testRecords = await _context.TestRecords
            .Include(t => t.Test)
            .Include(t => t.Trackings)
                .ThenInclude(tr => tr.Room)
            .Where(t => testRecordIds.Contains(t.TestRecordId))
            .ToListAsync();


        var items = invoiceDetails.Select(i =>
         {
             return new InvoiceDetailDisplayItem
             {
                 TestName = i.ItemName,
                 RoomName = "Quầy lễ tân",
                 Status = i.PaymentStatus,
                 CompletedAt = i.PaymentTime,
                 Amount = i.UnitPrice
             };
         }).ToList();


        var viewModel = new TotalInvoiceViewModel
        {
            Appointment = appointment,
            Items = items
        };

        // foreach (var item in viewModel.Items)
        // {
        //     Console.WriteLine(item.ToString());
        // }


        return View(viewModel);
    }
    
    [HttpGet]
    public async Task<IActionResult> InvoiceDetail(int id)
    {
        var invoice = await _context.InvoiceDetails
            .Include(i => i.Appointment)
                .ThenInclude(a => a.Patient)
            .FirstOrDefaultAsync(i => i.InvoiceDetailId == id);

        if (invoice == null)
        {
            TempData["error"] = "Không tìm thấy hóa đơn.";
            return RedirectToAction("ViewInvoiceList", "Tracking");
        }

        if (invoice.PaymentStatus == "Paid")
        {
            ViewBag.PaymentMethod = invoice.PaymentMethod;
            ViewBag.PaymentTime = invoice.PaymentTime;
            ViewBag.Message = "Hóa đơn đã thanh toán.";
            return View(invoice);
        }
        else
        {
            var paymentModel = new VnPayViewModel
            {
                Name = invoice.Appointment.Patient.FullName,
                Amount = invoice.UnitPrice,
                OrderDescription = $"Invoice:{invoice.InvoiceDetailId} - {invoice.ItemName}",
                OrderType = "other",
                InvoiceId = invoice.InvoiceDetailId
            };

            string paymentUrl = _vnPayService.CreatePaymentUrl(paymentModel, HttpContext);
            ViewBag.PaymentUrl = paymentUrl;

            return View(invoice);
        }
    }


}
