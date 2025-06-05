using Microsoft.EntityFrameworkCore;
using HospitalManagement.Repositories;
using HospitalManagement.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using HospitalManagement.Services;
using HospitalManagement.Models;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<HospitalManagementContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IDoctorRepository, DoctorRepository>();
builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<IStaffRepository, StaffRepository>();

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

builder.Services.AddDbContext<HospitalManagementContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDistributedMemoryCache();
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true); // máy ai nấy dùng


builder.Services.AddDistributedMemoryCache(); // Bộ nhớ tạm cho session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<IPasswordHasher<Patient>, PasswordHasher<Patient>>();

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

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Xử lý lỗi trang không tồn tại
app.UseStatusCodePagesWithReExecute("/Home/NotFound");

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
