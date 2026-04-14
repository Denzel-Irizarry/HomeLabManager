using Microsoft.EntityFrameworkCore;

using HomeLabManager.API.Infrastructure;
using HomeLabManager.API.Interfaces;
using HomeLabManager.API.Services;
using HomeLabManager.API.Services.Scraping;
using HomeLabManager.API.Services.Scraping.Interfaces;
using HomeLabManager.API.Services.Scraping.Providers;

namespace HomeLabManager.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Diagnostic logging removed
            var builder = WebApplication.CreateBuilder(args);

            //all builder services are a registry of when a service is requested, how to create an instance of that service.
            // Add services to the container.

            builder.Services.AddControllers();
            
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            //my services are build here a.k.a dependency injection?

            //Device service which handles the business logic for devices
            builder.Services.AddScoped<DeviceService>();

            //scanning service and interface
            builder.Services.AddScoped<ScanServiceInterface, ScanService>();

            // device repository which handles the database interactions for devices
            builder.Services.AddScoped<DeviceRepositoryInterface, DeviceRepository>();

            //vendor lookup service and test 
            builder.Services.AddScoped<VendorLookupInterface, FakeVendorLookupTest>();

            //this is for visually seeing the swagger ui and see how the json is formed
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            //Scraper service and interface
            builder.Services.AddScoped<IScraperService, ScraperService>();
      
            //Hardware lookup providers - this is where we can add multiple providers and the scraper service will use them in order until it finds a match
            builder.Services.AddHttpClient<UpcLookupProvider>();
            builder.Services.AddHttpClient<DellSerialLookupProvider>();
            builder.Services.AddHttpClient<CiscoSerialLookupProvider>();
            builder.Services.AddScoped<IHardwareLookupProvider, FakeHardwareLookupProvider>();
            builder.Services.AddScoped<IHardwareLookupProvider, UpcLookupProvider>();
            builder.Services.AddScoped<IHardwareLookupProvider, DellSerialLookupProvider>();
            builder.Services.AddScoped<IHardwareLookupProvider, FakeCiscoSerialLookupProvider>();
            builder.Services.AddScoped<IHardwareLookupProvider, CiscoSerialLookupProvider>();
            builder.Services.AddScoped<IHardwareLookupProvider, FakeSerialLookupProvider>();
            
            //ComponentRespositoryInterface, ComponentRepository: Maps interface to implementation
            builder.Services.AddScoped<ComponentRepositoryInterface, ComponentRepository>();
            //ComponentService: This is the service that will be used to handle the business logic for components
            builder.Services.AddScoped<ComponentService>();

            //DBcontext for build services to know to use SQLite
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<ApplicationDBContext>(options => options.UseSqlite(connectionString));

            //DeviceComponentRespositoryInterface, DeviceComponentRespository: Maps interface to implementation for device components
            builder.Services.AddScoped<DeviceComponentRepositoryInterface, DeviceComponentRepository>();
            builder.Services.AddScoped<DeviceComponentService>();

            var app = builder.Build();

            // Keep Swagger available for local debugging even when the app is started
            // directly from the build output and launchSettings are skipped.
            app.UseSwagger();
            app.UseSwaggerUI();

            if (!string.IsNullOrWhiteSpace(app.Configuration["ASPNETCORE_HTTPS_PORT"]))
            {
                app.UseHttpsRedirection();
            }

            app.UseAuthorization();

            app.MapGet("/", () => Results.Redirect("/swagger"));

            app.MapControllers();

            // Diagnostic logging removed
            app.Run();
        }
    }
}
