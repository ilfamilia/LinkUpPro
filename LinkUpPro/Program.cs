using LinkUpPro.Core.Application;
using LinkUpPro.Infrastructure.Identity;
using LinkUpPro.Infrastructure.Persistence;
using LinkUpPro.Infrastructure.Shared;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddPersistenceLayerIoc(builder.Configuration);
builder.Services.AddIdentityLayerIocForWebApp(builder.Configuration);
builder.Services.AddApplicationLayerIoc();
builder.Services.AddSharedLayerIoc(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
