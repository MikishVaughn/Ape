using Ape.Data;
using Ape.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Build connection string: use DB_*_ILLUSTRATE environment variables for production, fall back to appsettings for local dev
var dbServer = Environment.GetEnvironmentVariable("DB_SERVER_ILLUSTRATE");
var dbName = Environment.GetEnvironmentVariable("DB_NAME_ILLUSTRATE");
var dbUser = Environment.GetEnvironmentVariable("DB_USER_ILLUSTRATE");
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD_ILLUSTRATE");

string connectionString;
if (!string.IsNullOrEmpty(dbServer) && !string.IsNullOrEmpty(dbName))
{
    connectionString = $"Server={dbServer};Database={dbName};User Id={dbUser};Password={dbPassword};TrustServerCertificate=True;MultipleActiveResultSets=true";
}
else
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string not found. Set DB_SERVER_ILLUSTRATE/DB_NAME_ILLUSTRATE environment variables or configure ConnectionStrings:DefaultConnection in appsettings.json.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<CredentialEncryptionService>();
builder.Services.AddScoped<SecureConfigurationService>();
builder.Services.AddTransient<IEmailSender, EnhancedEmailService>();
builder.Services.AddTransient<EmailService>();
builder.Services.AddScoped<ISystemSettingsService, SystemSettingsService>();
builder.Services.AddScoped<IDocumentManagementService, DocumentManagementService>();
builder.Services.AddScoped<IImageOptimizationService, ImageOptimizationService>();
builder.Services.AddScoped<IGalleryManagementService, GalleryManagementService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Auto-apply migrations (creates database and tables on first run)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();
}

// Seed roles and admin user
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    string[] roles = ["Admin", "Member"];
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    // Seed a default admin account if no Admin users exist
    var admins = await userManager.GetUsersInRoleAsync("Admin");
    if (admins.Count == 0)
    {
        var adminEmail = builder.Configuration["AdminEmail"];
        if (string.IsNullOrEmpty(adminEmail))
            adminEmail = "admin@admin.com";

        var adminPassword = builder.Configuration["AdminPassword"];
        if (string.IsNullOrEmpty(adminPassword))
            adminPassword = "Admin123!";

        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new IdentityUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                await userManager.AddToRoleAsync(adminUser, "Member");
                logger.LogWarning("Default admin account created: {Email} — change the password after first login.", adminEmail);
            }
            else
            {
                logger.LogError("Failed to create default admin account: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Info/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

// Track user activity for the Active Users dashboard (must be after UseAuthorization)
app.UseMiddleware<Ape.Middleware.ActivityTrackingMiddleware>();

// UseStaticFiles serves runtime-uploaded files (e.g. gallery images) from wwwroot.
// MapStaticAssets handles build-time assets with fingerprinted caching.
app.UseStaticFiles();
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Info}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();
