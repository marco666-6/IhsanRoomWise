// Program.cs

using System;
using IhsanRoomWise.Functions;

var builder = WebApplication.CreateBuilder(args);

// Register the background service
builder.Services.AddHostedService<BkngStatusUpdHelperFunction>();

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8); // Set to 8 hours or any desired duration
    options.Cookie.HttpOnly = true;
    options.Cookie.Name = ".IhsanRoomWise.Session"; // <--- Add line
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

//app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=LoginView}/{id?}");

app.Run();