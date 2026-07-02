using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using KinoAnalyzer.Data;
using KinoAnalyzer.Services;

var builder = WebApplication.CreateBuilder(args);

// Razor Pages + Blazor
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// HttpClient y Scraper
builder.Services.AddHttpClient();
builder.Services.AddScoped<ScraperService>();
builder.Services.AddScoped<KinoStatsService>();

// Base de datos
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// Identity — sistema de usuarios
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
})
.AddEntityFrameworkStores<AppDbContext>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapBlazorHub();

app.Run();