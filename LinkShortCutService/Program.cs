using System.Text.Json.Serialization;

using LinkShortCutService.Data.Context;
using LinkShortCutService.Options;
using LinkShortCutService.Services;
using LinkShortCutService.Services.Interfaces;

using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;

services
   .AddScoped<ILinkManager, DbLinkManager>()
   .AddOptions<LinkManagerOptions>()
   .Bind(builder.Configuration.GetSection("LinkOptions"))
   .Validate(opt => opt.HashAlgorithm is "MD5" or "SHA256")
   .Validate(opt => opt.Encoding is "UTF-8" or "UTF-32" or "Unicode" or "ASCII")
   .Validate(opt => opt.ConcurrentDbTimeout > 0)
   .Validate(opt => opt.ConcurrentDbTryCount > 0)
    ;

services
   .AddControllersWithViews()
   .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.WriteIndented = true;
        opt.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
    });

services
   .AddEndpointsApiExplorer()
   .AddSwaggerGen();

services.AddDbContext<ContextDB>(opt => opt.UseSqlite(builder.Configuration.GetConnectionString("Sqlite")));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ContextDB>();
    var applied = await db.Database.GetAppliedMigrationsAsync();
    var pending = await db.Database.GetPendingMigrationsAsync();

    if (!applied.Any() && !pending.Any())
        await db.Database.EnsureCreatedAsync();
    else if (pending.Any())
        await db.Database.MigrateAsync();
}

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
