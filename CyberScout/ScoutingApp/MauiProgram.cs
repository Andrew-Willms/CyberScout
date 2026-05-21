using Database;
using Database.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using ScoutingApp.AppManagement;
using ScoutingApp.Views.Pages.Flyout;
using ScoutingApp.Views.Pages.Match;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace ScoutingApp;



public static class MauiProgram {

	public static MauiApp CreateMauiApp() {

		MauiAppBuilder builder = MauiApp.CreateBuilder();

		builder
			.UseMauiApp<App>()
			.UseBarcodeReader()
			.ConfigureFonts(fonts => {
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			})
			.ConfigureMauiHandlers(handlers => {
				handlers.AddHandler<CameraBarcodeReaderView, CameraBarcodeReaderViewHandler>();
				handlers.AddHandler<CameraView, CameraViewHandler>();
				handlers.AddHandler<BarcodeGeneratorView, BarcodeGeneratorViewHandler>();
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif
		builder.Services.AddSingleton<AppManagerGetter>();
		builder.Services.AddSingleton<IDataStoreCreator, SqliteDataStoreVersion1Creator>();
		builder.Services.AddSingleton<IErrorPresenter, ErrorPresenter>();

		// todo figure out when new instances of the transient types are being created and if
		// making them singleton would work and make things more performant.
		builder.Services.AddSingleton<SetupTab>();
		builder.Services.AddSingleton<AutoTab>();
		builder.Services.AddSingleton<TeleTab>();
		builder.Services.AddSingleton<EndgameTab>();
		builder.Services.AddSingleton<ConfirmTab>();
		builder.Services.AddSingleton<ScoutPage>();
		builder.Services.AddSingleton<EventPage>();
		builder.Services.AddSingleton<MatchDetailsPage>();
		builder.Services.AddSingleton<SavedMatchesPage>();
		builder.Services.AddTransient<QrCodeScanner>();

		return builder.Build();
	}

}