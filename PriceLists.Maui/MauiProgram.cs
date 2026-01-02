using Microsoft.Extensions.Logging;
using PriceLists.Core.Abstractions;
using PriceLists.Infrastructure.Services;
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

            builder.Services.AddSingleton<IExcelImportService, ExcelImportService>();
            builder.Services.AddSingleton<PreviewStore>();

            builder.Services.AddSingleton<AppShell>();
            builder.Services.AddSingleton<ListsPage>();
            builder.Services.AddTransient<ImportPreviewPage>();

            builder.Services.AddSingleton<ListsViewModel>();
            builder.Services.AddTransient<ImportPreviewViewModel>();

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
