using LinkUpPro.Core.Application;
using LinkUpPro.Infrastructure.Identity;
using LinkUpPro.Infrastructure.Persistence;
using LinkUpPro.Infrastructure.Share;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
// Adapt trusted Web identity and DTO mappings without exposing HTTP types to module services.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<LinkUpPro.Core.Application.Interfaces.ICurrentUserService, LinkUpPro.Services.CurrentUserService>();
builder.Services.AddScoped<LinkUpPro.Services.PostFeedBuilder>();
builder.Services.AddScoped<LinkUpPro.Filters.SocialExceptionFilter>();
builder.Services.AddScoped<LinkUpPro.Filters.ActiveAccountFilter>();
builder.Services.AddAutoMapper(cfg => { }, typeof(LinkUpPro.Mappings.SocialWebMappingProfile).Assembly);

builder.Services.AddSession(opt =>
{
    opt.IdleTimeout = TimeSpan.FromMinutes(60);
    opt.Cookie.HttpOnly = true;    
});

builder.Services.AddPersistenceLayerIoc(builder.Configuration);
builder.Services.AddIdentityLayerIocForWebApp(builder.Configuration);
builder.Services.AddApplicationLayerIoc();
builder.Services.AddSharedLayerIoc(builder.Configuration,
    builder.Environment.WebRootPath ?? Path.Combine(builder.Environment.ContentRootPath, "wwwroot"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();

/// <summary>Exposes the Web entry point to isolated MVC integration tests.</summary>
public partial class Program;
