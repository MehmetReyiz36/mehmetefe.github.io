using WEB.Services;


var builder = WebApplication.CreateBuilder(args);

// Servisleri ekle
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<GoogleSheetsService>();

var app = builder.Build();

// Pipeline yapýlandýrmasý
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();