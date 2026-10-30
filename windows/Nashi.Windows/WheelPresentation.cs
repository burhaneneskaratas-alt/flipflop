using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Nashi.Core;
namespace Nashi.Windows;
internal static class WheelPresentation
{
    internal const double Diameter=320;
    internal static double StartAngle(int count)=>count==3?-180:-90;
    internal static Preferences Preferences{get;set;}=new();
    // Each tool gets its own colour: a soft tint at rest and a brighter one under the pointer.
    private static readonly string[] LightTints=["#FFE0EA","#FFE9D1","#FFF3C4","#D6F7EA","#D8ECFF","#E6DEFF"];
    private static readonly string[] LightActive=["#FF8FB3","#FFB26B","#FFD84D","#5FE0B5","#7CC4FF","#B9A4FF"];
    private static readonly string[] DarkTints=["#3B1B38","#3D2A18","#3A3416","#16372F","#17304A","#2C2459"];
    private static readonly string[] DarkActive=["#A8336B","#B8641F","#A58A12","#1F8A68","#2A6FAE","#6249C9"];
    internal static Brush SectorFill(int index,bool active)
    {
        var palette=Theme.Dark?(active?DarkActive:DarkTints):(active?LightActive:LightTints);
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(palette[index%palette.Length]));
    }
    internal static Brush Backdrop{get{var brush=Theme.Brush("Surface").Clone();brush.Opacity=Preferences.WheelFinish=="glass"?.95:1;return brush;}}
    internal static double LabelRadius(int count,double half)=>count<=2?180:4*Math.Sin(half)*(Math.Pow(250,3)-Math.Pow(108,3))/(6*half*(250*250-108*108));
    internal static (StackPanel Label,Point Center) Caption(ToolChoice choice,Geometry petal,double angle,double half,int count)
    {
        var preferred=LabelRadius(count,half);
        for(var font=16d;font>=10;font-=.5)for(var width=142d;width>=72;width-=7)
        {
            var label=new StackPanel{Width=width,IsHitTestVisible=false};
            if(choice.Placement is {} placement){var glyph=new System.Windows.Controls.Canvas{Width=36,Height=25,HorizontalAlignment=HorizontalAlignment.Center,Margin=new(0,0,0,7)};glyph.Children.Add(new System.Windows.Shapes.Rectangle{Width=35,Height=24,RadiusX=3,RadiusY=3,Stroke=Theme.Brush("Ink"),StrokeThickness=1.7});var (start,end)=placement.Range();var fill=new System.Windows.Shapes.Rectangle{Width=29*(end-start),Height=18,Fill=Theme.Brush("Ink"),Opacity=.65};System.Windows.Controls.Canvas.SetLeft(fill,3+29*start);System.Windows.Controls.Canvas.SetTop(fill,3);glyph.Children.Add(fill);label.Children.Add(glyph);}
            else label.Children.Add(new System.Windows.Controls.TextBlock{Text=choice.Symbol,FontSize=32,TextAlignment=TextAlignment.Center,Foreground=Theme.Brush("Ink"),Margin=new(0,0,0,6)});
            label.Children.Add(new System.Windows.Controls.TextBlock{Text=L10n.Uppercase(choice.Title),FontSize=font,FontWeight=FontWeights.SemiBold,TextAlignment=TextAlignment.Center,TextWrapping=TextWrapping.Wrap,Foreground=Theme.Brush("Ink")});
            label.Measure(new Size(width,double.PositiveInfinity));var h=label.DesiredSize.Height;
            for(var step=0;step<=20;step++)
            {
                var radial=preferred+(step==0?0:(step%2==1?1:-1)*((step+1)/2)*5);var center=new Point(270+radial*Math.Cos(angle),270+radial*Math.Sin(angle));var r=new Rect(center.X-width/2-3,center.Y-h/2-3,width+6,h+6);
                if(new[]{r.TopLeft,r.TopRight,r.BottomLeft,r.BottomRight,new Point(r.Left,r.Top+r.Height/2),new Point(r.Right,r.Top+r.Height/2)}.All(petal.FillContains))return(label,center);
            }
        }
        var fallback=new StackPanel{Width=72};fallback.Children.Add(new System.Windows.Controls.TextBlock{Text=L10n.Uppercase(choice.Title),FontSize=10,TextAlignment=TextAlignment.Center,TextWrapping=TextWrapping.Wrap,Foreground=Theme.Brush("Ink")});fallback.Measure(new Size(72,double.PositiveInfinity));return(fallback,new Point(270+preferred*Math.Cos(angle),270+preferred*Math.Sin(angle)));
    }
    // Icon (or placement diagram) above a wrapped title, shrunk until it fits its tile.
    internal static StackPanel TileLabel(ToolChoice choice,double width,double height)
    {
        StackPanel? label=null;
        for(var font=14d;font>=9;font-=.5)
        {
            label=new StackPanel{Width=Math.Max(40,width-16),IsHitTestVisible=false,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};
            if(choice.Placement is {} placement){var glyph=new System.Windows.Controls.Canvas{Width=36,Height=25,HorizontalAlignment=HorizontalAlignment.Center,Margin=new(0,0,0,7)};glyph.Children.Add(new System.Windows.Shapes.Rectangle{Width=35,Height=24,RadiusX=3,RadiusY=3,Stroke=Theme.Brush("Ink"),StrokeThickness=1.7});var (start,end)=placement.Range();var fill=new System.Windows.Shapes.Rectangle{Width=29*(end-start),Height=18,Fill=Theme.Brush("Ink"),Opacity=.65};System.Windows.Controls.Canvas.SetLeft(fill,3+29*start);System.Windows.Controls.Canvas.SetTop(fill,3);glyph.Children.Add(fill);label.Children.Add(glyph);}
            else label.Children.Add(new System.Windows.Controls.TextBlock{Text=choice.Symbol,FontSize=Math.Min(30,height*.26),TextAlignment=TextAlignment.Center,Foreground=Theme.Brush("Ink"),Margin=new(0,0,0,4)});
            label.Children.Add(new System.Windows.Controls.TextBlock{Text=L10n.Text(choice.Title),FontSize=font,FontWeight=FontWeights.SemiBold,TextAlignment=TextAlignment.Center,TextWrapping=TextWrapping.Wrap,Foreground=Theme.Brush("Ink")});
            label.Measure(new Size(label.Width,double.PositiveInfinity));
            if(label.DesiredSize.Height<=height-14)break;
        }
        return label!;
    }
    internal static void Animate(Window window,double from=0){if(Preferences.Animations&&SystemParameters.ClientAreaAnimation)window.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(from,1,TimeSpan.FromMilliseconds(140)));}
}
// Where each tool tile sits on the 540 x 540 wheel canvas, and which tile a point falls in.
internal static class WheelLayout
{
    internal static IReadOnlyList<Rect> Cells(int count)
    {
        if(count<=0)return [];
        var columns=count<=3?count:count==4?2:count<=9?3:4;var rows=(count+columns-1)/columns;
        const double gap=14,left=34,top=96,width=472,height=408;
        var w=Math.Min(190,(width-gap*(columns-1))/columns);var h=Math.Min(150,(height-gap*(rows-1))/rows);
        var gridWidth=w*columns+gap*(columns-1);var gridHeight=h*rows+gap*(rows-1);var x0=left+(width-gridWidth)/2;var y0=top+(height-gridHeight)/2;
        var cells=new List<Rect>(count);
        for(var i=0;i<count;i++)
        {
            var row=i/columns;var column=i%columns;var inRow=row==rows-1?count-row*columns:columns;
            // A short last row is centered under the full ones.
            var rowWidth=w*inRow+gap*(inRow-1);var x=x0+(gridWidth-rowWidth)/2+column*(w+gap);
            cells.Add(new Rect(x,y0+row*(h+gap),w,h));
        }
        return cells;
    }
    internal static int IndexAt(Point point,int count)
    {
        var cells=Cells(count);
        for(var i=0;i<cells.Count;i++)if(cells[i].Contains(point))return i;
        return -1;
    }
}
