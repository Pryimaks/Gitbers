using System.Net.Http.Headers;
using Gitbers.Data;
using Gitbers.Models;
using Gitbers.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString),
        mysqlOptions =>
        {
            mysqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
        });
});

builder.Services.AddControllersWithViews();


// ============================================================
// GITHUB
// ============================================================

var githubToken =
    builder.Configuration["GitHub:Token"];

builder.Services.AddHttpClient<GitHubService>(client =>
{
    client.BaseAddress =
        new Uri("https://api.github.com/");

    client.DefaultRequestHeaders.Add(
        "Accept",
        "application/vnd.github+json");

    client.DefaultRequestHeaders.Add(
        "X-GitHub-Api-Version",
        "2026-03-10");

    client.DefaultRequestHeaders.Add(
        "User-Agent",
        "Gitbers");

    if (!string.IsNullOrWhiteSpace(githubToken))
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                githubToken);
    }
});


// ============================================================
// SERVICES
// ============================================================

builder.Services.AddScoped<MetricsService>();

builder.Services.AddScoped<
    IPasswordHasher<User>,
    PasswordHasher<User>>();


// ============================================================
// SESSION
// ============================================================

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout =
        TimeSpan.FromHours(2);

    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});


// ============================================================
// AUTHENTICATION
// ============================================================

builder.Services
    .AddAuthentication(
        CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath =
            "/Account/Login";

        options.AccessDeniedPath =
            "/Account/AccessDenied";

        options.ExpireTimeSpan =
            TimeSpan.FromHours(8);

        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();


// ============================================================
// APPLICATION
// ============================================================

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
    pattern:
        "{controller=Home}/{action=Index}/{id?}");

app.Run();