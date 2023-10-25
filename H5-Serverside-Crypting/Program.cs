using H5_Serverside_Crypting.Areas.Identity;
using H5_Serverside_Crypting.Data;
using H5_Serverside_Crypting.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.X509Certificates;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DbConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<IdentityUser>>();
builder.Services.AddSingleton<WeatherForecastService>();

//Service added to startup ---
builder.Services.AddScoped<toDoService>();
//End ----

//added context to startup
builder.Services.AddDbContext<toDoContext>(options =>
    options.UseSqlServer(connectionString));
// End ----

//Dataprotection service
builder.Services.AddDataProtection();
// End ----


//Inserted first and changedt certification name.
builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureHttpsDefaults(options =>
    {
        // Grab the secret value from the secret store.
        string? secretValue = builder.Configuration.GetValue<string>("KestrelCertificatePassword");
        options.ServerCertificate = new X509Certificate2("tec-h5-JB.pfx", secretValue);
    });
});
//----end insert ---




var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
