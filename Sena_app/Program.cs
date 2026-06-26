using Microsoft.EntityFrameworkCore;
using Sena_app.Components;
using Sena_app.Data;
using Sena_app.Services;
using Sena_app.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddScoped<ReminderService>();
builder.Services.AddSingleton<NotificationService>();
builder.Services.AddDataProtection();
builder.Services.AddControllers();
builder.Services.AddScoped<GoogleCalendarService>();

// DbContextFactory permite crear contextos en servicios Scoped y Singleton
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Scoped: un AuthService y TaskService por circuito Blazor
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<TaskService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();
app.MapControllers();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
