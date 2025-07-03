using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using EyeCareManagement.Data;
using EyeCareManagement.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=EyeCareManagement.db";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("EmployeeOnly", policy => policy.RequireRole("Employee", "Admin"));
    options.AddPolicy("DoctorOnly", policy => policy.RequireRole("Doctor", "Admin"));
    options.AddPolicy("OpticianOnly", policy => policy.RequireRole("Optician", "Admin"));
    options.AddPolicy("DoctorOrEmployee", policy => policy.RequireRole("Doctor", "Employee", "Admin"));
    options.AddPolicy("OpticianOrDoctor", policy => policy.RequireRole("Optician", "Doctor", "Admin"));
});

var app = builder.Build();

// Configure the HTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// Initialize database and seed data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        await context.Database.EnsureCreatedAsync();
        await SeedData(userManager, roleManager, context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();

static async Task SeedData(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext context)
{
    // Create roles
    string[] roleNames = { "Admin", "Employee", "Doctor", "Optician" };
    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    // Create default admin user
    if (await userManager.FindByEmailAsync("admin@eyecare.com") == null)
    {
        var adminUser = new ApplicationUser
        {
            UserName = "admin@eyecare.com",
            Email = "admin@eyecare.com",
            FirstName = "System",
            LastName = "Administrator",
            Role = UserRole.Admin,
            EmailConfirmed = true
        };

        await userManager.CreateAsync(adminUser, "Admin123!");
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }

    // Create sample doctor
    if (await userManager.FindByEmailAsync("doctor@eyecare.com") == null)
    {
        var doctorUser = new ApplicationUser
        {
            UserName = "doctor@eyecare.com",
            Email = "doctor@eyecare.com",
            FirstName = "Dr. John",
            LastName = "Smith",
            Role = UserRole.Doctor,
            EmailConfirmed = true
        };

        await userManager.CreateAsync(doctorUser, "Doctor123!");
        await userManager.AddToRoleAsync(doctorUser, "Doctor");
    }

    // Create sample employee
    if (await userManager.FindByEmailAsync("employee@eyecare.com") == null)
    {
        var employeeUser = new ApplicationUser
        {
            UserName = "employee@eyecare.com",
            Email = "employee@eyecare.com",
            FirstName = "Sarah",
            LastName = "Johnson",
            Role = UserRole.Employee,
            EmailConfirmed = true
        };

        await userManager.CreateAsync(employeeUser, "Employee123!");
        await userManager.AddToRoleAsync(employeeUser, "Employee");
    }

    // Create sample optician
    if (await userManager.FindByEmailAsync("optician@eyecare.com") == null)
    {
        var opticianUser = new ApplicationUser
        {
            UserName = "optician@eyecare.com",
            Email = "optician@eyecare.com",
            FirstName = "Mike",
            LastName = "Wilson",
            Role = UserRole.Optician,
            EmailConfirmed = true
        };

        await userManager.CreateAsync(opticianUser, "Optician123!");
        await userManager.AddToRoleAsync(opticianUser, "Optician");
    }

    // Seed Wards
    if (!context.Wards.Any())
    {
        var wards = new List<Ward>
        {
            new Ward { Name = "General Ward", Capacity = 20, Description = "General patient accommodation", Type = WardType.General },
            new Ward { Name = "ICU", Capacity = 10, Description = "Intensive Care Unit", Type = WardType.ICU },
            new Ward { Name = "Surgery Ward", Capacity = 15, Description = "Post-surgery recovery", Type = WardType.Surgery },
            new Ward { Name = "Recovery Ward", Capacity = 12, Description = "Patient recovery area", Type = WardType.Recovery },
            new Ward { Name = "Emergency Ward", Capacity = 8, Description = "Emergency patient care", Type = WardType.Emergency }
        };

        context.Wards.AddRange(wards);
        await context.SaveChangesAsync();
    }

    // Seed sample patients
    if (!context.Patients.Any())
    {
        var patients = new List<Patient>
        {
            new Patient
            {
                FirstName = "Emily",
                LastName = "Davis",
                Email = "emily.davis@email.com",
                PhoneNumber = "555-0101",
                DateOfBirth = new DateTime(1985, 5, 15),
                Gender = Gender.Female,
                Address = "123 Main St, Anytown, USA",
                EmergencyContact = "Robert Davis",
                EmergencyContactPhone = "555-0102"
            },
            new Patient
            {
                FirstName = "Michael",
                LastName = "Brown",
                Email = "michael.brown@email.com",
                PhoneNumber = "555-0201",
                DateOfBirth = new DateTime(1978, 8, 22),
                Gender = Gender.Male,
                Address = "456 Oak Ave, Somewhere, USA",
                EmergencyContact = "Lisa Brown",
                EmergencyContactPhone = "555-0202"
            },
            new Patient
            {
                FirstName = "Jessica",
                LastName = "Wilson",
                Email = "jessica.wilson@email.com",
                PhoneNumber = "555-0301",
                DateOfBirth = new DateTime(1992, 3, 10),
                Gender = Gender.Female,
                Address = "789 Pine Rd, Elsewhere, USA",
                EmergencyContact = "Mark Wilson",
                EmergencyContactPhone = "555-0302"
            }
        };

        context.Patients.AddRange(patients);
        await context.SaveChangesAsync();
    }
}