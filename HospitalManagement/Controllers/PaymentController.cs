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
using System.Security.Cryptography;
using System.Text;
using System.Linq;
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

    [HttpGet]
    public async Task<IActionResult> PayStartAppointmentForPatient(int? packageId, int? serviceId)
    {
        var invoicedetail = await _context.InvoiceDetails
            .Include(x => x.Appointment)
            .FirstOrDefaultAsync(a => (a.ItemType == "Package" && a.ItemId==packageId) || (a.ItemType == "Service" && a.ItemId==serviceId));


        if (invoicedetail == null)
            return NotFound();

        var paymentModel = new VnPayViewModel
        {
            Name = "Name",
            Amount = invoicedetail.UnitPrice,
            OrderDescription = $"{invoicedetail.ItemType} - {invoicedetail.ItemName}",
            OrderType = "other",
            InvoiceId = invoicedetail.InvoiceDetailId
        };

        string paymentUrl = _vnPayService.CreatePaymentUrl(paymentModel, HttpContext);
        ViewBag.PaymentUrl = paymentUrl;
        ViewBag.InvoiceDetailId = invoicedetail.InvoiceDetailId;
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
    public IActionResult PaymentCallbackVnpay()
    {
        var vnpayData = Request.Query;
        var responseData = new SortedList<string, string>();

        // Check chữ ký hợp lệ
        var isValidSignature = ValidateVnpaySignature(vnpayData, _configuration["Vnpay:HashSecret"]);
        if (!isValidSignature)
        {
            TempData["error"] = "Chữ ký không hợp lệ. Nghi ngờ gian lận!";
            return RedirectToAction("Index", "Home");
        }
        
        foreach (var key in vnpayData.Keys)
        {
            if (key.StartsWith("vnp_"))
            {
                responseData.Add(key, vnpayData[key]);
            }
        }

        string responseCode = responseData["vnp_ResponseCode"];
        string orderInfo = responseData["vnp_OrderInfo"];
        int invoiceId = 0;

        var match = Regex.Match(orderInfo, @"Invoice:(\d+)");
        if (match.Success)
        {
            invoiceId = int.Parse(match.Groups[1].Value);
        }

        if (invoiceId <= 0) return NotFound();

        var invoiceDetail = _context.InvoiceDetails
            .Include(i => i.Appointment)
            .FirstOrDefault(i => i.InvoiceDetailId == invoiceId);

        if (invoiceDetail == null) return NotFound();

        if (invoiceDetail.PaymentStatus == "Paid")
        {
            TempData["error"] = "Nghi ngờ giao dịch gian lận.";
            return NotFound();
        }

        if (responseCode == "00")
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

            TempData["success"] = "Thanh toán thành công.";
        }
        else
        {
            TempData["error"] = $"Giao dịch thất bại. Mã lỗi: {responseCode}";
        }

        // 👉 Redirect về trang chủ sau khi xử lý xong
        return RedirectToAction("Index", "Home");
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
    
    public bool ValidateVnpaySignature(IQueryCollection vnpayData, string secretKey)
    {
        var inputData = new SortedList<string, string>();
        foreach (var key in vnpayData.Keys)
        {
            if (key.StartsWith("vnp_") && key != "vnp_SecureHash" && key != "vnp_SecureHashType")
            {
                inputData.Add(key, vnpayData[key]);
            }
        }

        var rawData = string.Join("&", inputData.Select(x => $"{x.Key}={WebUtility.UrlEncode(x.Value)}"));
        var hashBytes = new HMACSHA512(Encoding.UTF8.GetBytes(secretKey)).ComputeHash(Encoding.UTF8.GetBytes(rawData));
        var computedHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

        var receivedHash = vnpayData["vnp_SecureHash"].ToString().ToLower();

        return computedHash == receivedHash;
    }


}
