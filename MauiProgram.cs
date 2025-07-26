using MauiCreditCardView.CustomControls;
using Microsoft.Extensions.Logging;
#if IOS
using UIKit;
#endif

namespace MauiCreditCardView;

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
				fonts.AddFont("Poppins-SemiBold.ttf", "poppins500");
                fonts.AddFont("Poppins-Medium.ttf", "poppins400");
			}).ConfigureMauiHandlers(handlers =>
			{
				Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping(nameof(CustomEntry), (handler, view) =>
				{
#if ANDROID
					handler.PlatformView.SetBackgroundColor(Android.Graphics.Color.Transparent);
					handler.PlatformView.SetPadding(0, 0, 0, 0);
#elif IOS
                    handler.PlatformView.BackgroundColor = UIKit.UIColor.Clear;
                    handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
                    if (UIDevice.CurrentDevice.CheckSystemVersion(16, 0))
                    {
                        handler.PlatformView.InputAccessoryView = CreateDoneToolbar((s, e) =>
                        {
                            handler.PlatformView?.ResignFirstResponder();
                            if (view is CustomEntry customEntry)
                                customEntry.Unfocus();
                        });
                    }
#endif
				});
			});
#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
	#if IOS
    public static UIKit.UIToolbar CreateDoneToolbar(EventHandler onDoneTapped)
    {
        var toolbar = new UIKit.UIToolbar { Translucent = true };
        toolbar.SizeToFit();

        var doneButton = new UIKit.UIBarButtonItem(
            UIKit.UIBarButtonSystemItem.Done, onDoneTapped);

        if (UIDevice.CurrentDevice.CheckSystemVersion(16, 0))
        {
            var space = new UIKit.UIBarButtonItem(
                UIKit.UIBarButtonSystemItem.FlexibleSpace, null, null);
            toolbar.SetItems(new[] { space, doneButton }, false);
        }
        else
        {
            toolbar.SetItems(new[] { doneButton }, false);
        }

        return toolbar;
    }
#endif
}
