using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using PureWave.Admin.Components;
using PureWave.Admin.Models;
using PureWave.Admin.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AdminAuthSettings>(builder.Configuration.GetSection(AdminAuthSettings.SectionName));
builder.Services.Configure<MySqlSettings>(builder.Configuration.GetSection(MySqlSettings.SectionName));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
        options.Cookie.Name = "PureWaveAdmin.Auth";
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped(sp => sp.GetRequiredService<IOptions<MySqlSettings>>().Value);
builder.Services.AddScoped<AdminRepository>();
builder.Services.AddSingleton<AdminSchemaInitializer>();
builder.Services.AddSingleton<ReportTemplateService>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/login/submit", async (
    HttpContext httpContext,
    IOptions<AdminAuthSettings> options) =>
{
    var form = await httpContext.Request.ReadFormAsync();
    var username = form["username"].ToString();
    var password = form["password"].ToString();
    var settings = options.Value;

    if (!string.Equals(username, settings.Username, StringComparison.Ordinal) ||
        !string.Equals(password, settings.Password, StringComparison.Ordinal))
    {
        return Results.Redirect("/login?error=1");
    }

    var claims = new[]
    {
        new Claim(ClaimTypes.Name, username),
        new Claim(ClaimTypes.Role, "Admin")
    };

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);

    await httpContext.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        principal,
        new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(12)
        });

    return Results.Redirect("/");
});

app.MapPost("/logout/submit", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<AdminSchemaInitializer>();
    await initializer.EnsureInitializedAsync();
}

app.Run();
