using Microsoft.Extensions.Logging;

namespace StudyMap;

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
			#if ANDROID
			builder.ConfigureMauiHandlers(handlers =>
			{
				handlers.AddHandler(
					typeof(Shell),
					typeof(Platforms.Android.StudyMapShellRenderer));
			});
			#endif

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
