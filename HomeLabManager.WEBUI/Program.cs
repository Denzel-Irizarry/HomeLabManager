using HomeLabManager.WEBUI.Components;

namespace HomeLabManager.WEBUI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Diagnostic logging removed
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

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
