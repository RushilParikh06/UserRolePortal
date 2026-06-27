using Microsoft.EntityFrameworkCore;
using UserRolePortal.Models;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = "wwwroot"
});

// Add MVC Controllers and Views
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<UserRolePortal.Filters.ActivityLogFilter>();
});

// Add IHttpContextAccessor for accessing HttpContext in views
builder.Services.AddHttpContextAccessor();

// Add database context configuration
builder.Services.AddDbContext<ApponDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Session Services
builder.Services.AddDistributedMemoryCache();

// Add session support
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add services to the container.


builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);


        // TO ENSURE BROWSER ACCEPTS THE COOKIE
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
    });


builder.Services.AddSingleton<UserRolePortal.Data.DbConnectionFactory>(); //dapper

builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    // Set the max multipart body length to 2MB globally
    options.MultipartBodyLengthLimit = 2 * 1024 * 1024;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}
else
{
    // Show detailed errors in development
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
