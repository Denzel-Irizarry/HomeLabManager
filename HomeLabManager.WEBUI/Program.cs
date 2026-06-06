using HomeLabManager.WEBUI.Components;
using Microsoft.AspNetCore.DataProtection;

namespace HomeLabManager.WEBUI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Diagnostic logging removed
            var builder = WebApplication.CreateBuilder(args);
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.AddDebug();

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            builder.Services.AddSignalR(options =>
            {
                options.MaximumReceiveMessageSize = 20 * 1024 * 1024;
            });

            if (builder.Environment.IsDevelopment())
            {
                var keyRingPath = Path.Combine(builder.Environment.ContentRootPath, ".data-protection-keys");
                builder.Services.AddDataProtection()
                    .PersistKeysToFileSystem(new DirectoryInfo(keyRingPath));
            }

            var apiBaseUrl = builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5015";

            // Connect the frontend UI to the backend API through a named client.
            builder.Services.AddHttpClient("HomeLabApi", client =>
            {
                client.BaseAddress = new Uri(apiBaseUrl);
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

            if (!string.IsNullOrWhiteSpace(app.Configuration["ASPNETCORE_HTTPS_PORT"]))
            {
                app.UseHttpsRedirection();
            }

            app.UseAntiforgery();

            app.MapStaticAssets();
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            // Diagnostic logging removed
            app.Run();
        }
    }
}
