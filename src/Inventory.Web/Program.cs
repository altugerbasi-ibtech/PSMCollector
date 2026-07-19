using Inventory.Web.Components;
using Inventory.Infrastructure;
using Microsoft.AspNetCore.Authentication.Negotiate;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme).AddNegotiate();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Reader", policy => policy.RequireAuthenticatedUser())
    .AddPolicy("Operator", policy => policy.RequireAssertion(context => IsInConfiguredGroup(context.User, builder.Configuration, "Authorization:OperatorGroups") || IsInConfiguredGroup(context.User, builder.Configuration, "Authorization:AdminGroups")))
    .AddPolicy("Admin", policy => policy.RequireAssertion(context => IsInConfiguredGroup(context.User, builder.Configuration, "Authorization:AdminGroups")));
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddInventoryInfrastructure(builder.Configuration.GetConnectionString("InventoryDatabase") ?? throw new InvalidOperationException("InventoryDatabase connection string is required."));
builder.Services.AddSingleton<Inventory.Web.Services.TestConnectionPoolService>();
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapHealthChecks("/health");

app.Run();

static bool IsInConfiguredGroup(System.Security.Claims.ClaimsPrincipal user, IConfiguration configuration, string key) =>
    configuration.GetSection(key).Get<string[]>()?.Any(user.IsInRole) == true;
