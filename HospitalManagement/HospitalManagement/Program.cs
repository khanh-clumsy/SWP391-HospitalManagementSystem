using Microsoft.EntityFrameworkCore;
using HospitalManagement.Repositories;
using HospitalManagement.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using HospitalManagement.Services;
using HospitalManagement.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using HospitalManagement.Filters;
using OfficeOpenXml;
using HospitalManagement.Services.VnPay;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<HospitalManagementContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IDoctorRepository, DoctorRepository>();

// Đăng ký các Repository
builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
builder.Services.AddScoped<ITrackingRepository, TrackingRepository>();
builder.Services.AddScoped<IRoomRepository, RoomRepository>();
builder.Services.AddScoped<ITestRepository, TestRepository>();
builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<IStaffRepository, StaffRepository>();
builder.Services.AddScoped<INewsRepository, NewsRepository>();
builder.Services.AddScoped<IPackageRepository, PackageRepository>();
builder.Services.AddScoped<ISlotRepository, SlotRepository>();
builder.Services.AddScoped<IScheduleRepository, ScheduleRepository>();
builder.Services.AddScoped<IScheduleChangeRepository, ScheduleChangeRepository>();
builder.Services.AddScoped<InvoiceService>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IFeedbackRepository, FeedbackRepository>();
ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;


// Add services to the container
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin() // Cho phép bất kỳ origin nào
              .AllowAnyMethod()  // Cho phép bất kỳ phương thức HTTP (GET, POST, PUT, DELETE, v.v.)
              .AllowAnyHeader(); // Cho phép bất kỳ header nào
    });
});

// Cấu hình Authentication và Authorization
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/Auth/Login";
    options.AccessDeniedPath = "/Home/AccessDenied";
})
.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
{
    options.ClientId = builder.Configuration["GoogleKeys:ClientId"];
    options.ClientSecret = builder.Configuration["GoogleKeys:ClientSecret"];
    options.CallbackPath = "/signin-google";

    options.Events.OnRemoteFailure = context =>
    {
        var error = context.Request.Query["error"].ToString();

        if (!string.IsNullOrEmpty(error))
        {
            context.Response.Redirect("/Auth/Login?error=Fail%20To%20Login%20Google");
        }

        context.HandleResponse();
        return Task.CompletedTask;
    };


    options.Events.OnRedirectToAuthorizationEndpoint = context =>
    {
        var redirectUri = context.RedirectUri;
        redirectUri += (redirectUri.Contains("?") ? "&" : "?") + "prompt=select_account";
        context.Response.Redirect(redirectUri);
        return Task.CompletedTask;
    };
});

// Authorization
builder.Services.AddAuthorization();

// Các dịch vụ khác
builder.Services.AddControllersWithViews()
    .AddNewtonsoftJson();

builder.Services.AddDistributedMemoryCache();
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true); // máy ai nấy dùng

builder.Services.AddSingleton<BookingQueueService>();
builder.Services.AddHostedService<BookingProcessor>();
builder.Services.AddDistributedMemoryCache(); // Bộ nhớ tạm cho session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<IPasswordHasher<Patient>, PasswordHasher<Patient>>();

//builder.Services.AddControllersWithViews(options =>
//{
//    options.Filters.Add(new PreventSpamAttribute { Seconds = 1 }); // mặc định trong filters là 1s
//});

builder.Services.AddScoped<IVnPayService, VnPayService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<HospitalManagementContext>();
    try
    {
        if (context.Database.CanConnect())
        {
            Console.WriteLine("YES SQL Server connection successful!");
        }
        else
        {
            Console.WriteLine("NO SQL Server connection failed.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error Exception when connecting to database: " + ex.Message);
    }
}
using (var scope = app.Services.CreateScope())
{
    var repo = scope.ServiceProvider.GetRequiredService<IScheduleRepository>();
    repo.PrintDoctorRoomsToday();
}
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Xử lý lỗi trang không tồn tại
app.UseStatusCodePagesWithRedirects("/Home/NotFound");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseSession();

app.UseRouting();

app.UseAuthentication();  // PHẢI thêm dòng này
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();