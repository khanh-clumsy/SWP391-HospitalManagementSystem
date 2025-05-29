using HospitalManagement.Data;
using HospitalManagement.Models;
using HospitalManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
﻿using System.Threading.Tasks;
using HospitalManagement.Data;
using HospitalManagement.Models;
using HospitalManagement.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using X.PagedList;



namespace HospitalManagement.Controllers
{
    public class PatientController : Controller
    {
        private readonly HospitalManagementContext _context;
        private readonly IDoctorRepository _doctorRepo;

        public PatientController(HospitalManagementContext context, IDoctorRepository doctorRepo)
        {
            _context = context;
            _doctorRepo = doctorRepo;

        }

        /**
         * Controller for ViewDoctors page, get name, department, exp year, isHead,
         * sort type, to filter out doctor, handle pagination
         */
        public async Task<IActionResult> ViewDoctors(int? page, string? name, string? department, int? exp, bool? isHead, string? sort)
        {
            int pageSize = 8;
            int pageNumber = page ?? 1;

            // Lấy danh sách bác sĩ theo trang, async với EF Core
            var doctors = await _doctorRepo.SearchAsync(name, department, exp, isHead, sort, pageNumber, pageSize);
            // Lấy tổng số bác sĩ
            var totalDoctors = await _doctorRepo.CountAsync(name, department, exp, isHead);

            // Tạo IPagedList từ danh sách đã lấy
            var pagedDoctors = new StaticPagedList<Doctor>(doctors, pageNumber, pageSize, totalDoctors);
            var departments = await _doctorRepo.GetDistinctDepartment();


            // Truyền lại dữ liệu vào cshtml để khi reload trang filter vẫn hiển thị nội dung filter đã chọn
            ViewBag.Name = name;
            ViewBag.Department = department;
            ViewBag.Experience = exp;
            ViewBag.Type = isHead;
            ViewBag.Sort = sort;
            ViewBag.Departments = departments;

           
            return View(pagedDoctors);
        }
        public async Task<IActionResult> DoctorDetail(int id)
        {
            var doctor = await _doctorRepo.GetByIdAsync(id);
            if (doctor == null)
            {
                return NotFound();
            }
            return View(doctor);
        }


        [HttpGet]
        public async Task<IActionResult> BookingAppointment(int? doctorId)
        {
            var userJson = HttpContext.Session.GetString("UserSession");

            if (string.IsNullOrEmpty(userJson))
            {
                return RedirectToAction("Login", "Auth");
            }

            var user = JsonConvert.DeserializeObject<Account>(userJson);
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }
            var doctors = await _context.Doctors
           .Select(d => new SelectListItem
           {
               Value = d.DoctorId.ToString(),
               Text = d.Account.FullName
           })
           .ToListAsync();

            // Lấy danh sách slot từ DB
            var slots = await _context.Slots
                .Select(s => new SelectListItem
                {
                    Value = s.SlotId.ToString(),
                    Text = s.StartTime.ToString(@"hh\:mm") + " - " + s.EndTime.ToString(@"hh\:mm")
                })
                .ToListAsync();


            var model = new BookingApointment
            {
                Name = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                DoctorOptions = doctors,
                SlotOptions = slots,
                AppointmentDate = DateTime.Today,
                SelectedDoctorId = doctorId ?? 0  
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookingAppointment(BookingApointment model)
        {
            ModelState.Remove(nameof(model.DoctorOptions));
            ModelState.Remove(nameof(model.SlotOptions));
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                foreach (var error in errors)
                {
                    // Ghi log các lỗi
                    Console.WriteLine(error);
                }
                // Nạp lại danh sách dropdown khi trả view để dropdown hiển thị đúng
                model.DoctorOptions = await _context.Doctors
                    .Select(d => new SelectListItem
                    {
                        Value = d.DoctorId.ToString(),
                        Text = d.Account.FullName
                    })
                    .ToListAsync();

                model.SlotOptions = await _context.Slots
                    .Select(s => new SelectListItem
                    {
                        Value = s.SlotId.ToString(),
                        Text = s.StartTime.ToString(@"hh\:mm") + " - " + s.EndTime.ToString(@"hh\:mm")
                    })
                    .ToListAsync();
                return View(model);
            }
            //var userJson = HttpContext.Session.GetString("UserSession");
            //if (string.IsNullOrEmpty(userJson))
            //{
            //    return RedirectToAction("Login", "Auth");
            //}

            //var user = JsonConvert.DeserializeObject<Account>(userJson);
            //if (user == null)
            //{
            //    return RedirectToAction("Login", "Auth");
            //}

            var patient = _context.Patients.FirstOrDefault(p => p.PatientId == user.PatientId);

            var appointment = new Appointment
            {
                PatientId = patient.PatientId,
                DoctorId = model.SelectedDoctorId,
                Note = model.Note,
                SlotId = model.SelectedSlotId,
                ServiceId = model.SelectedServiceId,
                Date = DateOnly.FromDateTime(model.AppointmentDate),
                Status = "Pending",
            };

            _context.Appointments.Add(appointment);
            _context.SaveChanges();
            return RedirectToAction("ViewBookingAppointment");
        }

        public IActionResult ViewBookingAppointment(string searchName, string timeFilter, DateTime? dateFilter, string statusFilter)
        {
            var appointments = _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Include(a => a.Slot)
            .AsQueryable();

            // Lọc theo thời gian slot

            if (!string.IsNullOrEmpty(timeFilter) && TimeOnly.TryParse(timeFilter, out var parsedTime))
            {
                appointments = appointments.Where(a => a.Slot.StartTime == parsedTime);
            }

            // Lọc theo ngày
            if (dateFilter.HasValue)
            {
                var filterDate = DateOnly.FromDateTime(dateFilter.Value);
                appointments = appointments.Where(a => a.Date == filterDate);
            }

            // Lọc theo trạng thái
            if (!string.IsNullOrEmpty(statusFilter))
            {
                appointments = appointments.Where(a => a.Status == statusFilter);
            }

            return View(appointments.ToList());
        }


       


    }

}
