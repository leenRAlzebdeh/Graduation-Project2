using JUSTLockers.DataBase;
using JUSTLockers.Service;
using JUSTLockers.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

/*

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add the database connection factory
builder.Services.AddSingleton<IDbConnectionFactory>(provider =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new Exception("There is no connection");
    return new DbConnectionFactory(connectionString);
});
builder.Services.AddScoped<AdminService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Login}/{id?}")
    .WithStaticAssets();

app.Run();
*/
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<DbConnectionFactory>(optons => optons.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"), new MySqlServerVersion(new Version(8, 0, 36))));
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<SupervisorService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<UserActions>();
builder.Services.AddScoped<CabinetService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddSession(
    options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(20); 
        options.Cookie.HttpOnly = true; 
        options.Cookie.IsEssential = true; 
    }
    );
builder.Services.AddAuthentication("MyCookieAuth")
    .AddCookie("MyCookieAuth", options =>
    {
        options.LoginPath = "/Home/Login";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(20); // optional
        options.SlidingExpiration = true;
        options.Cookie.IsEssential = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });

builder.Services.AddAuthorization();
builder.Services.AddDistributedMemoryCache();

var app = builder.Build();
app.UseSession();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Login}/{id?}");

app.Run();
