using Microsoft.EntityFrameworkCore;
using HospitalManagement.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.Extensions.Options;
using HospitalManagement.Services;

var builder = WebApplication.CreateBuilder(args);


builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true); // máy ai nấy dùng

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<HospitalManagementContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Auth/Login"; 
    options.AccessDeniedPath = "/Auth/AccessDenied"; 
});
builder.Services.AddDistributedMemoryCache(); // Bộ nhớ tạm cho session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // thời gian sống của session
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddControllersWithViews().AddNewtonsoftJson();
builder.Services.AddScoped<EmailService>();

//Configuration Login Google Account
try
{
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    }).AddCookie().AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
    {
        options.ClientId = builder.Configuration.GetSection("GoogleKeys:ClientId").Value;
        options.ClientSecret = builder.Configuration.GetSection("GoogleKeys:ClientSecret").Value;

        // luôn hỏi để chọn gmail
        options.Events.OnRedirectToAuthorizationEndpoint = context =>
        {
            var redirectUri = context.RedirectUri;
            redirectUri += (redirectUri.Contains("?") ? "&" : "?") + "prompt=select_account";
            context.Response.Redirect(redirectUri);
            return Task.CompletedTask;
        };
    });
}
catch (Exception ex)
{
    Console.WriteLine("Error Exception when connecting to database: " + ex.Message);
    Console.WriteLine("Google Client ID: " + builder.Configuration["GoogleKeys:ClientId"]);

}


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

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession(); // Thêm middleware này trước UseEndpoints hoặc UseRouting

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();
