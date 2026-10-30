using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Media.Imaging;
using Nashi.Core;
namespace Nashi.Windows;
public sealed record ToolChoice(string Id,string Title,string Symbol,WindowPlacement? Placement=null);
public sealed class WheelWindow:Window
{
    private readonly List<Button> buttons=[];
    private readonly List<System.Windows.Shapes.Rectangle> sectors=[];
    private readonly IReadOnlyList<ToolChoice> choices;
    private readonly Action<ToolChoice> select;
    private readonly Canvas surface;
    private readonly int tileCount;
    private int selected=-1;
    internal int SelectedIndex=>selected;
    internal void HoverScreen(Point point)=>Highlight(WheelLayout.IndexAt(surface.PointFromScreen(point),tileCount));
    internal bool Navigate(Key key)
    {
        if(key==Key.Escape){Close();return true;}
        if(buttons.Count==0)return false;
        if(key is Key.Right or Key.Down){Highlight((selected+1)%buttons.Count);buttons[selected].Focus();return true;}
        if(key is Key.Left or Key.Up){Highlight((selected-1+buttons.Count)%buttons.Count);buttons[selected].Focus();return true;}
        if(key is Key.Enter or Key.Space){var choice=choices[Math.Max(0,selected)];Close();select(choice);return true;}
        return false;
    }
    private void Highlight(int index)
    {
        if(selected==index)return;selected=index;
        for(var i=0;i<sectors.Count;i++){sectors[i].Fill=WheelPresentation.SectorFill(i,i==index);sectors[i].Stroke=i==index?Theme.Brush("Gold"):null;}
        if(index>=0)Feedback.Play();
    }
    public WheelWindow(Window? owner,IReadOnlyList<ToolChoice> tools,Action<ToolChoice> select,bool externalDrag=false)
    {
        this.choices=tools;this.select=select;if(owner is not null)Owner=owner;Title=Nashi.Core.Brand.AppName+(externalDrag?" Window Management":" tools");Width=WheelPresentation.Diameter*WheelPresentation.Preferences.WheelScale;Height=Width;WindowStyle=WindowStyle.None;AllowsTransparency=true;Background=Brushes.Transparent;ResizeMode=ResizeMode.NoResize;ShowInTaskbar=false;WindowStartupLocation=externalDrag?WindowStartupLocation.Manual:WindowStartupLocation.CenterOwner;
        if(externalDrag){ShowActivated=false;Topmost=true;Focusable=false;}
        var canvas=new Canvas{Width=540,Height=540};surface=canvas;KeyboardNavigation.SetTabNavigation(canvas,KeyboardNavigationMode.Cycle);Content=new Viewbox{Child=canvas,Stretch=Stretch.Uniform};
        var panel=new Border{Width=530,Height=530,CornerRadius=new(46),Background=WheelPresentation.Backdrop,BorderThickness=new(1),IsHitTestVisible=false};panel.SetResourceReference(Border.BorderBrushProperty,"Line");Canvas.SetLeft(panel,5);Canvas.SetTop(panel,5);canvas.Children.Add(panel);
        var count=Math.Min(12,tools.Count);tileCount=count;var cells=WheelLayout.Cells(count);
        for(var i=0;i<count;i++)
        {
            var choice=tools[i];var cell=cells[i];
            var tile=new System.Windows.Shapes.Rectangle{Width=cell.Width,Height=cell.Height,RadiusX=24,RadiusY=24,StrokeThickness=3,Fill=WheelPresentation.SectorFill(i,false),Tag="tile"};Canvas.SetLeft(tile,cell.X);Canvas.SetTop(tile,cell.Y);canvas.Children.Add(tile);sectors.Add(tile);
            var label=WheelPresentation.TileLabel(choice,cell.Width,cell.Height);
            var button=new Button{Style=(Style)Application.Current.FindResource("WheelLabelButton"),Content=label,Width=cell.Width,Height=cell.Height,Background=Brushes.Transparent,Foreground=Theme.Brush("Ink"),Padding=new(0),BorderThickness=new(0)};Canvas.SetLeft(button,cell.X);Canvas.SetTop(button,cell.Y);canvas.Children.Add(button);buttons.Add(button);var index=i;System.Windows.Automation.AutomationProperties.SetName(button,L10n.Text(choice.Title));
            button.Click+=(_,_)=>{Close();select(choice);};button.MouseEnter+=(_,_)=>Highlight(index);button.GotKeyboardFocus+=(_,_)=>Highlight(index);
            tile.MouseLeftButtonUp+=(_,_)=>{Close();select(choice);};tile.MouseEnter+=(_,_)=>Highlight(index);
        }
        var heading=new StackPanel{Width=540,IsHitTestVisible=false};heading.Children.Add(L10n.Label(new TextBlock{Text=externalDrag?"Arrange window":tools.All(t=>t.Id.StartsWith("convert:"))?"Convert":tools.Any(t=>t.Id=="float")?"File actions":"Tools",FontSize=externalDrag?17:21,FontWeight=FontWeights.SemiBold,TextAlignment=TextAlignment.Center,Foreground=Theme.Brush("Ink")}));heading.Children.Add(L10n.Label(new TextBlock{Text=externalDrag?"Release to place · Esc cancels":"Esc to close",FontSize=externalDrag?9:11,TextAlignment=TextAlignment.Center,Foreground=Theme.Brush("Muted"),Margin=new(0,5,0,0)}));Canvas.SetLeft(heading,0);Canvas.SetTop(heading,30);canvas.Children.Add(heading);
        PreviewKeyDown+=(_,e)=>{e.Handled=Navigate(e.Key);};Loaded+=(_,_)=>{WheelPresentation.Animate(this);if(!externalDrag&&buttons.Count>0)buttons[0].Focus();};bool closing=false;Closing+=(_,_)=>closing=true;Deactivated+=(_,_)=>{if(!externalDrag&&!closing&&IsVisible)Close();};
    }
}
internal static class Feedback
{
    internal static bool Enabled{get;set;}=true;
    private static MediaPlayer? player;private static DateTime last;
    internal static void Stop(){player?.Stop();player?.Close();player=null;}
    public static void Play()
    {
        if(!Enabled||(DateTime.UtcNow-last).TotalMilliseconds<70)return;last=DateTime.UtcNow;
        try
        {
            var directory=Nashi.Core.Brand.DataRoot;Directory.CreateDirectory(directory);var file=Path.Combine(directory,"WheelSelection.wav");
            if(!File.Exists(file)){using var source=Application.GetResourceStream(new Uri("pack://application:,,,/Assets/WheelSelection.wav")).Stream;using var target=File.Create(file);source.CopyTo(target);}
            if(player is null){var created=new MediaPlayer{Volume=.65};player=created;created.MediaOpened+=(_,_)=>{if(Enabled&&ReferenceEquals(player,created))created.Play();};created.Open(new Uri(file));}else{player.Position=TimeSpan.Zero;player.Play();}

        }catch(IOException){}catch(InvalidOperationException){}
    }
}
