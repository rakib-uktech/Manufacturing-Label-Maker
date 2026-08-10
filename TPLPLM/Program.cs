using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NetSuite;
using System.Security.Principal;
using TPLPLM;
using TPLPLM.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(provider =>
{
    var config = builder.Configuration.GetSection("NetSuiteConfig");
    return new NetSuiteClient(
        config["ConsumerKey"],
        config["ConsumerSecret"],
        config["AccessToken"],
        config["TokenSecret"],
        config["Realm"],
        config["BaseUrl"]
    );
});



// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Add authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

// Register the NetSuiteClient service
builder.Services.AddHttpClient();

builder.Services.AddRazorPages();



var app = builder.Build();

// Configure the HTTP request pipeline.

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
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
// Seed roles and admin user
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await SeedData.Initialize(services);
}

app.Run();
