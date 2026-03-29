using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using PureWave.Web.Components;
using PureWave.Web.Models;
using PureWave.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/admin/login";
        options.AccessDeniedPath = "/admin/login";
        options.Cookie.Name = "PureWave.AdminAuth";
    });

builder.Services.AddAuthorization();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<IIntakeSubmissionStore, FileIntakeSubmissionStore>();
builder.Services.Configure<AdminAuthSettings>(
    builder.Configuration.GetSection(AdminAuthSettings.SectionName));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapGet("/admin/intakes/download/{fileName}", async (
    string fileName,
    IIntakeSubmissionStore intakeSubmissionStore,
    CancellationToken cancellationToken) =>
{
    try
    {
        var file = await intakeSubmissionStore.OpenReadAsync(fileName, cancellationToken);
        return Results.File(file.Stream, "application/json", file.DownloadName);
    }
    catch (FileNotFoundException)
    {
        return Results.NotFound();
    }
}).RequireAuthorization();

app.MapPost("/admin/login", async (
    HttpContext context,
    IConfiguration configuration,
    CancellationToken cancellationToken) =>
{
    var form = await context.Request.ReadFormAsync(cancellationToken);
    var username = form["username"].ToString();
    var password = form["password"].ToString();

    var settings = configuration.GetSection(AdminAuthSettings.SectionName).Get<AdminAuthSettings>() ?? new AdminAuthSettings();

    if (string.Equals(username, settings.Username, StringComparison.Ordinal) &&
        !string.IsNullOrWhiteSpace(settings.Password) &&
        string.Equals(password, settings.Password, StringComparison.Ordinal))
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, username),
            new(ClaimTypes.Role, "Admin")
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        return Results.Redirect("/admin/intakes");
    }

    return Results.Redirect("/admin/login?error=1");
});

app.MapPost("/admin/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/admin/login");
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
