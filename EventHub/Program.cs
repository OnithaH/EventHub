using EventHub.Data;
using EventHub.Services.Interfaces;
using EventHub.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
// Add services to the container
builder.Services.AddControllersWithViews();

// Configure Entity Framework with PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString, o => o.SetPostgresVersion(13, 0)));

// Register application services with dependency injection
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IQRCodeService, QRCodeService>();

// Configure session with enhanced security
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Session expires after 30 minutes of inactivity
    options.Cookie.HttpOnly = true; // Prevent XSS attacks
    options.Cookie.IsEssential = true; // Required for GDPR compliance
    options.Cookie.SameSite = SameSiteMode.Strict; // CSRF protection
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // HTTPS in production
    options.Cookie.Name = "EventHub.Session"; // Custom session cookie name
});

// Configure cookie policy for enhanced security
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => true; // GDPR compliance
    options.MinimumSameSitePolicy = SameSiteMode.Strict;
    options.HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always;
    options.Secure = CookieSecurePolicy.SameAsRequest;
});

// Configure forwarded headers for reverse proxy scenarios
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

// Add Anti-forgery token configuration
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "EventHub.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// Configure logging
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();

    if (builder.Environment.IsProduction())
    {
        //logging.AddApplicationInsights(); // Add Application Insights in production
    }
});

// Configure HSTS for production
if (builder.Environment.IsProduction())
{
    builder.Services.AddHsts(options =>
    {
        options.Preload = true;
        options.IncludeSubDomains = true;
        options.MaxAge = TimeSpan.FromDays(365);
    });
}

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts(); // Enable HSTS in production
}

// Security headers middleware
app.Use(async (context, next) =>
{
    // Add security headers
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

    // Content Security Policy - FIXED
    var csp = "default-src 'self'; " +
              "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdnjs.cloudflare.com https://cdn.jsdelivr.net https://code.jquery.com; " +
              "style-src 'self' 'unsafe-inline' https://cdnjs.cloudflare.com https://cdn.jsdelivr.net https://fonts.googleapis.com; " +
              "img-src 'self' data: https:; " +
              "font-src 'self' https://cdnjs.cloudflare.com https://cdn.jsdelivr.net https://fonts.gstatic.com; " +
              "connect-src 'self' wss://localhost:* https://localhost:*; " +
              "frame-ancestors 'none';";

    context.Response.Headers.Append("Content-Security-Policy", csp);

    await next();
});

// Configure middleware pipeline
app.UseForwardedHeaders(); // Handle forwarded headers from reverse proxy
app.UseHttpsRedirection(); // Redirect HTTP to HTTPS
app.UseStaticFiles(); // Serve static files

app.UseRouting(); // Enable routing

app.UseCookiePolicy(); // Apply cookie policy
app.UseSession(); // Enable sessions (must be after UseRouting and before UseAuthorization)

app.UseAuthentication(); // Enable authentication (if using ASP.NET Core Identity in future)
app.UseAuthorization(); // Enable authorization

// Configure default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Custom error pages for specific status codes
app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");

// Database initialization and migration
try
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Ensure database is created and migrations are applied
        if (app.Environment.IsDevelopment())
        {
            context.Database.EnsureCreated();
        }
        else
        {
            context.Database.Migrate(); // Apply migrations in production
        }

        // Seed initial data if needed
        await SeedInitialData(context);
    }
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred while initializing the database");
}

app.Run();

// Helper method to seed initial data
static async Task SeedInitialData(ApplicationDbContext context)
{
    // Check if data already exists
    if (context.Users.Any())
        return;

    // Create default admin user (optional)
    /*
    var adminUser = new User
    {
        Name = "System Administrator",
        Email = "admin@eventhub.com",
        Password = "TempPassword123!", // Will be hashed by UserService
        Role = UserRole.Admin,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };
    
    context.Users.Add(adminUser);
    await context.SaveChangesAsync();
    */
}