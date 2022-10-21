using LinkShortCutService.Data.Context;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;

services
   .AddControllersWithViews()
   .AddJsonOptions(opt => opt.JsonSerializerOptions.WriteIndented = true);

services
   .AddEndpointsApiExplorer()
   .AddSwaggerGen();

services.AddDbContext<ContextDB>(opt => opt.UseSqlite(builder.Configuration.GetConnectionString("Sqlite")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

//app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
