using JUSTLockers.DataBase;
using JUSTLockers.Service;
using JUSTLockers.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();
builder.Services.AddDbContext<DbConnectionFactory>(optons => optons.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"), new MySqlServerVersion(new Version(8, 0, 36))));
builder.Services.AddSingleton<AdminService>();
builder.Services.AddScoped<SupervisorService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<UserActions>();
builder.Services.AddScoped<CabinetService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddSingleton<IEmailService, EmailService>();
builder.Services.AddHostedService<SemesterEndService>();
builder.Services.AddSession(
    options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(10); 
        options.Cookie.HttpOnly = true; 
        options.Cookie.IsEssential = true;   
    }
    );
builder.Services.AddAuthentication("MyCookieAuth")
    .AddCookie("MyCookieAuth", options =>
    {
        options.LoginPath = "/Home/Login";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
        options.SlidingExpiration = true;
        options.Cookie.IsEssential = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.None;
    });

builder.Services.AddAuthorization();
builder.Services.AddDistributedMemoryCache();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();


app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Login}/{id?}");

app.Run();
