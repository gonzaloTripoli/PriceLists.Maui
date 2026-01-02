using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PriceLists.Core.Abstractions;
using PriceLists.Infrastructure.Persistence;
using PriceLists.Infrastructure.Services;
using PriceLists.Infrastructure.Repositories;
using PriceLists.Maui.Services;
using PriceLists.Maui.ViewModels;
using PriceLists.Maui.Views;

namespace PriceLists.Maui
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            var dbPath = SqliteDbPathProvider.GetDbPath();
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));

            builder.Services.AddSingleton<IExcelImportService, ExcelImportService>();
            builder.Services.AddScoped<IPriceListRepository, PriceListRepository>();
            builder.Services.AddScoped<IPriceListService, PriceListService>();
            builder.Services.AddSingleton<PreviewStore>();

            builder.Services.AddSingleton<AppShell>();
            builder.Services.AddSingleton<ListsPage>();
            builder.Services.AddTransient<ImportPreviewPage>();
            builder.Services.AddTransient<ListDetailPage>();

            builder.Services.AddSingleton<ListsViewModel>();
            builder.Services.AddTransient<ImportPreviewViewModel>();
            builder.Services.AddTransient<ListDetailViewModel>();

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                dbContext.Database.Migrate();
            }

            return app;
        }
    }
}
