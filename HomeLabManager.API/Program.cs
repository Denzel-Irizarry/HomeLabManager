
using HomeLabManager.API.Infrastructure;
using HomeLabManager.API.Interfaces;
using HomeLabManager.API.Services;


namespace HomeLabManager.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            //my services are build here a.k.a dependency injection?

            //Device service which handles the input from the northbound api to southbound api
            builder.Services.AddScoped<DeviceService>();

            //scanning service
            builder.Services.AddScoped<ScanServiceInterface, FakeScanServiceTesting>();

            //vendor lookup service and test 
            builder.Services.AddScoped<VendorLookupInterface, FakeVendorLookupTest>();

            //this is for visually seeing the swagger ui and see how the json is formed
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                //comment out for seeing the swagger ui
                //app.MapOpenApi();
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
