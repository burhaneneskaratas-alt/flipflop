using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
namespace Nashi.Windows;

internal static class Theme
{
    internal static bool Dark{get;private set;}
    internal static Brush Primary=>new LinearGradientBrush(Color.FromRgb(255,79,139),Color.FromRgb(255,159,67),0);
    internal static Brush Brush(string name)=>(Brush)Application.Current.FindResource(name);
    internal static void Apply(string appearance)
    {
        Dark=appearance=="dark"||(appearance=="system"&&Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize","AppsUseLightTheme",1) is int value&&value==0);
        foreach(var (key,light,dark) in new[]{("Paper","#F5F2FF","#14112B"),("Ink","#1C1646","#F1EEFF"),("Surface","#FFFFFF","#1F1B40"),("Inset","#ECE7FF","#19163A"),("Muted","#5F5A8C","#B3AEDB"),("Line","#D8D0FF","#38326A"),("Sage","#CFF5E6","#17423A"),("Sand","#FFE3A6","#3F2C74"),("Gold","#FF4F8B","#FF6FA5"),("GoldText","#C2255C","#FF8FB3")})
            Application.Current.Resources[key]=new SolidColorBrush((Color)ColorConverter.ConvertFromString(Dark?dark:light));
        foreach(Window window in Application.Current.Windows)ApplyTitleBar(window);
    }
    internal static void Attach(Window window)
    {
        window.SetResourceReference(Window.BackgroundProperty,"Paper");window.SetResourceReference(Window.ForegroundProperty,"Ink");
        window.Loaded+=(_,_)=>{if(window.WindowStyle!=WindowStyle.None)DesktopGeometry.FitToWorkArea(window);L10n.Bind(window,Window.TitleProperty,window.Title);L10n.Attach(window);L10n.LabelFields(window);};
        window.SourceInitialized+=(_,_)=>ApplyTitleBar(window);
    }
    internal static void Entrance(UIElement element)
    {
        if(!WheelPresentation.Preferences.Animations||!SystemParameters.ClientAreaAnimation)return;
        element.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(180)));
        var offset=new TranslateTransform();element.RenderTransform=offset;
        offset.BeginAnimation(TranslateTransform.YProperty,new DoubleAnimation(8,0,TimeSpan.FromMilliseconds(180)){DecelerationRatio=.8});
    }
    private static void ApplyTitleBar(Window window)
    {
        var handle=new WindowInteropHelper(window).Handle;if(handle==IntPtr.Zero)return;var dark=Dark?1:0;_ = DwmSetWindowAttribute(handle,20,ref dark,sizeof(int));
        // Windows 11 only: tint the title bar with the page colour so it reads as part of the window.
        if(Application.Current.TryFindResource("Paper") is SolidColorBrush paper&&Application.Current.TryFindResource("Ink") is SolidColorBrush ink){var caption=ColorRef(paper.Color);var text=ColorRef(ink.Color);_ = DwmSetWindowAttribute(handle,35,ref caption,sizeof(int));_ = DwmSetWindowAttribute(handle,36,ref text,sizeof(int));}
    }
    private static int ColorRef(Color color)=>color.R|(color.G<<8)|(color.B<<16);
    [DllImport("dwmapi.dll")]private static extern int DwmSetWindowAttribute(IntPtr window,int attribute,ref int value,int size);
}
