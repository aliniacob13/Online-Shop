using Microsoft.EntityFrameworkCore;
using OnlineShop.Data;
using OnlineShop.Models;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

//connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

//DbContext pe MySQL

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        connectionString,
        new MySqlServerVersion(new Version(8, 4)),
        mySqlOptions => mySqlOptions.EnableRetryOnFailure()
    )
);

//DbContext pe SQL server
/*
builder.Services.AddDbContext<ApplicationDbContext>
    (options => options.UseSqlServer(connectionString));
*/
//Identity
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false; // nu cerem confirmarea prin email
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultUI(); 

//MVC + Razor Pages
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// PASUL 5 - useri si roluri
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    SeedData.Initialize(services);
}

//pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

//imp pentru Identity
app.UseAuthentication();
app.UseAuthorization();

//rute MVC
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

//rute pentru paginile Identity (/Identity/Account/Register, Login )
app.MapRazorPages();

app.Run();