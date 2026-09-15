using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OnlineMarketplace.Database;
using OnlineMarketplace.Hubs;
using OnlineMarketplace.Models;
using System.Net.Http.Headers;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDistributedMemoryCache(); 
builder.Services.AddSession(options =>
{
	options.IdleTimeout = TimeSpan.FromMinutes(30);
	options.Cookie.HttpOnly = true;
	options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient("PayMongo", client =>
{
	client.BaseAddress = new Uri("https://api.paymongo.com/v1/");

	var secretKey = builder.Configuration["PayMongo:SecretKey"];
	if (!string.IsNullOrEmpty(secretKey))
	{
		var authHeaderValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{secretKey}:"));
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeaderValue);
	}
});

builder.Services.AddSignalR();

builder.Services.AddScoped<IPasswordHasher<ApplicationUserModel>, PasswordHasher<ApplicationUserModel>>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
	options.UseNpgsql(builder.Configuration.GetConnectionString("ApplicationDbContext")));

var app = builder.Build();


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}


app.UseHttpsRedirection();
app.UseStaticFiles();

app.MapHub<ChatHub>("/chatHub");

app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
